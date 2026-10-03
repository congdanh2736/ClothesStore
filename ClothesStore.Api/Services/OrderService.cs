using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OrderService(IOrderRepository repository, ApplicationDbContext context, IMapper mapper)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDetailDTO>> GetAllAsync()
        => _mapper.Map<IEnumerable<OrderDetailDTO>>(await _repository.GetAllAsync());

    public async Task<OrderDetailDTO?> GetByIdAsync(int id)
    {
        var order = await _repository.GetByIdWithDetailsAsync(id);
        return order is null ? null : _mapper.Map<OrderDetailDTO>(order);
    }

    public async Task<(bool Success, string? Error, OrderDetailDTO? Data)> CreateAsync(CreateOrderRequest request)
    {
        var error = await ValidateOrderRequestAsync(request.CustomerId, request.AddressId, request.PromotionId, request.Items);
        if (error is not null)
            return (false, error, null);

        var items = await BuildItemsAsync(request.Items!);
        var order = _mapper.Map<Order>(request);
        order.Status = "Pending";
        order.TotalAmount = items.Sum(item => item.Price * item.Quantity);
        order.OrderItems = items;
        order.PaymentTransaction = new PaymentTransaction
        {
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "COD" : request.PaymentMethod.Trim(),
            Status = "Pending"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var createdOrder = await _repository.GetByIdWithDetailsAsync(order.Id);
        return (true, null, _mapper.Map<OrderDetailDTO>(createdOrder));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateOrderRequest request)
    {
        var order = await _repository.GetByIdWithDetailsAsync(id);
        if (order is null)
            return (false, "Không tìm thấy đơn hàng.");

        var error = await ValidateOrderRequestAsync(order.CustomerId, request.AddressId, request.PromotionId, request.Items, allowUnchangedItems: true);
        if (error is not null)
            return (false, error);

        order.AddressId = request.AddressId;
        order.PromotionId = request.PromotionId;

        if (request.Items is not null)
        {
            var items = await BuildItemsAsync(request.Items.Select(item => new CreateOrderItemRequest
            {
                VariantId = item.VariantId,
                Quantity = item.Quantity
            }));

            _context.OrderItems.RemoveRange(order.OrderItems);
            order.OrderItems = items;
            order.TotalAmount = items.Sum(item => item.Price * item.Quantity);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order is null)
            return false;

        await _repository.DeleteAsync(order);
        return true;
    }

    private async Task<string?> ValidateOrderRequestAsync(
        int customerId,
        int addressId,
        int promotionId,
        IEnumerable<UpdateOrderItemRequest>? items,
        bool allowUnchangedItems)
    {
        if (!await _context.Customers.AnyAsync(customer => customer.Id == customerId))
            return "Không tìm thấy khách hàng.";

        if (!await _context.Addresses.AnyAsync(address => address.Id == addressId && address.CustomerId == customerId))
            return "Địa chỉ không thuộc về khách hàng này.";

        var now = DateTime.UtcNow;
        if (!await _context.Promotions.AnyAsync(promotion =>
                promotion.Id == promotionId && promotion.StartDate <= now && promotion.EndDate >= now))
            return "Khuyến mãi không tồn tại hoặc không còn hiệu lực.";

        if (items is null)
            return allowUnchangedItems ? null : "Đơn hàng phải có ít nhất một sản phẩm.";

        var itemList = items.ToList();
        if (itemList.Count == 0 || itemList.Any(item => item.Quantity <= 0))
            return "Đơn hàng phải có ít nhất một sản phẩm với số lượng hợp lệ.";

        var variantIds = itemList.Select(item => item.VariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants.CountAsync(variant => variantIds.Contains(variant.VariantId));
        return variantCount == variantIds.Count ? null : "Phiên bản sản phẩm không tồn tại.";
    }

    private Task<string?> ValidateOrderRequestAsync(
        int customerId,
        int addressId,
        int promotionId,
        IEnumerable<CreateOrderItemRequest>? items)
        => ValidateOrderRequestAsync(
            customerId,
            addressId,
            promotionId,
            items?.Select(item => new UpdateOrderItemRequest { VariantId = item.VariantId, Quantity = item.Quantity }),
            allowUnchangedItems: false);

    private async Task<List<OrderItem>> BuildItemsAsync(IEnumerable<CreateOrderItemRequest> requests)
    {
        var requestList = requests.ToList();
        var variantIds = requestList.Select(item => item.VariantId).Distinct().ToList();
        var variants = await _context.ProductVariants
            .Where(variant => variantIds.Contains(variant.VariantId))
            .ToDictionaryAsync(variant => variant.VariantId);

        return requestList.Select(item => new OrderItem
        {
            VariantId = item.VariantId,
            Quantity = item.Quantity,
            Price = (decimal)variants[item.VariantId].Price
        }).ToList();
    }
}
