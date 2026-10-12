using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] OrderStatus, OrderPricing, BusinessException
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

/*
 * [THÊM] Ghi chú nghiệp vụ - Sửa từng dòng của đơn hàng.
 * Chỉ cho phép khi đơn còn Pending; mỗi thay đổi đều cập nhật tồn kho của cửa hàng xuất kho
 * và tính lại tổng tiền (có trừ khuyến mãi).
 */
public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventoryService _inventory; // [THÊM] cập nhật tồn kho khi thêm/sửa/xóa dòng

    // [SỬA] thêm tham số IInventoryService
    public OrderItemService(IOrderItemRepository repository, ApplicationDbContext context, IMapper mapper, IInventoryService inventory)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
        _inventory = inventory; // [THÊM]
    }

    public async Task<IEnumerable<OrderItemDTO>> GetAllAsync()
        => _mapper.Map<IEnumerable<OrderItemDTO>>(await _repository.GetAllAsync());

    public async Task<OrderItemDTO?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdWithDetailsAsync(id);
        return item is null ? null : _mapper.Map<OrderItemDTO>(item);
    }

    public async Task<(bool Success, string? Error, OrderItemDTO? Data)> CreateAsync(CreateOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "Số lượng phải lớn hơn 0.", null);

        // [SỬA] Load đơn kèm items + promotion (trước đây chỉ kiểm tra đơn tồn tại)
        var order = await LoadOrderAsync(request.OrderId);
        if (order is null)
            return (false, "Không tìm thấy đơn hàng.", null);
        // [THÊM] Chỉ thêm sản phẩm vào đơn còn Pending
        if (order.Status != OrderStatus.Pending)
            return (false, "Chỉ được thêm sản phẩm khi đơn hàng đang chờ xác nhận (Pending).", null);

        var variant = await _context.ProductVariants.FindAsync(request.VariantId);
        if (variant is null)
            return (false, "Phiên bản sản phẩm không tồn tại.", null);

        try
        {
            var item = await _context.InTransactionAsync(async () =>
            {
                // [THÊM] Trừ tồn kho tại cửa hàng xuất kho
                if (order.StoreId.HasValue)
                    await _inventory.DecreaseStockAsync(order.StoreId.Value, new[] { new StockLine(variant.VariantId, request.Quantity) });

                // [THÊM] Biến thể đã có trong đơn -> cộng dồn số lượng
                var existing = order.OrderItems.FirstOrDefault(i => i.VariantId == variant.VariantId);
                if (existing is not null)
                {
                    existing.Quantity += request.Quantity;
                }
                else
                {
                    existing = new OrderItem
                    {
                        OrderId = order.Id,
                        VariantId = variant.VariantId,
                        Quantity = request.Quantity,
                        Price = variant.Price // [SỬA] bỏ ép kiểu (decimal) vì Price đã đổi sang decimal
                    };
                    order.OrderItems.Add(existing);
                }

                // [SỬA] thay RecalculateOrderTotalAsync cũ: tính lại tổng tiền có trừ khuyến mãi
                OrderPricing.Recalculate(order);
                await _context.SaveChangesAsync();
                return existing;
            });

            var createdItem = await _repository.GetByIdWithDetailsAsync(item.Id);
            return (true, null, _mapper.Map<OrderItemDTO>(createdItem));
        }
        catch (BusinessException ex) // [THÊM] hết hàng... -> trả lỗi 400
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "Số lượng phải lớn hơn 0.");

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return (false, "Không tìm thấy chi tiết đơn hàng.");

        // [THÊM] Chỉ sửa dòng của đơn còn Pending
        var order = await LoadOrderAsync(item.OrderId);
        if (order is null || order.Status != OrderStatus.Pending)
            return (false, "Chỉ được sửa sản phẩm khi đơn hàng đang chờ xác nhận (Pending).");

        var variant = await _context.ProductVariants.FindAsync(request.VariantId);
        if (variant is null)
            return (false, "Phiên bản sản phẩm không tồn tại.");

        // [THÊM] Không cho đổi sang biến thể đã có ở dòng khác của cùng đơn
        if (variant.VariantId != item.VariantId && order.OrderItems.Any(i => i.Id != item.Id && i.VariantId == variant.VariantId))
            return (false, "Biến thể này đã có trong đơn, hãy sửa số lượng ở dòng đó.");

        try
        {
            await _context.InTransactionAsync(async () =>
            {
                if (order.StoreId.HasValue)
                {
                    // [THÊM] Hoàn kho dòng cũ, trừ kho dòng mới
                    await _inventory.IncreaseStockAsync(order.StoreId.Value, new[] { new StockLine(item.VariantId, item.Quantity) });
                    await _inventory.DecreaseStockAsync(order.StoreId.Value, new[] { new StockLine(variant.VariantId, request.Quantity) });
                }

                // [SỬA] Chỉ lấy giá mới khi đổi biến thể (trước đây luôn ghi đè giá -> đơn cũ bị đổi giá khi sửa số lượng)
                if (item.VariantId != variant.VariantId)
                    item.Price = variant.Price;

                item.VariantId = variant.VariantId;
                item.Quantity = request.Quantity;

                // [SỬA] tính lại tổng tiền có trừ khuyến mãi
                OrderPricing.Recalculate(order);
                await _context.SaveChangesAsync();
            });
        }
        catch (BusinessException ex) // [THÊM]
        {
            return (false, ex.Message);
        }

        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return false;

        // [THÊM] Chỉ xóa dòng của đơn còn Pending, và đơn phải còn ít nhất 1 sản phẩm
        var order = await LoadOrderAsync(item.OrderId);
        if (order is null || order.Status != OrderStatus.Pending)
            throw new BusinessException("Chỉ được xóa sản phẩm khi đơn hàng đang chờ xác nhận (Pending).");
        if (order.OrderItems.Count <= 1)
            throw new BusinessException("Đơn hàng phải còn ít nhất một sản phẩm. Hãy hủy đơn nếu không mua nữa.");

        await _context.InTransactionAsync(async () =>
        {
            // [THÊM] Hoàn kho số lượng của dòng bị xóa
            if (order.StoreId.HasValue)
                await _inventory.IncreaseStockAsync(order.StoreId.Value, new[] { new StockLine(item.VariantId, item.Quantity) });

            order.OrderItems.Remove(item);
            _context.OrderItems.Remove(item);

            OrderPricing.Recalculate(order); // [SỬA] tính lại tổng tiền có trừ khuyến mãi
            await _context.SaveChangesAsync();
        });

        return true;
    }

    // [THÊM] Load đơn kèm OrderItems + Promotion để tính tiền
    // [XÓA] RecalculateOrderTotalAsync cũ (không trừ khuyến mãi) -> thay bằng OrderPricing.Recalculate
    private async Task<Order?> LoadOrderAsync(int orderId)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Promotion)
            .FirstOrDefaultAsync(o => o.Id == orderId);
}
