using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OrderItemService(IOrderItemRepository repository, ApplicationDbContext context, IMapper mapper)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
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

        if (!await _context.Orders.AnyAsync(order => order.Id == request.OrderId))
            return (false, "Không tìm thấy đơn hàng.", null);

        var variant = await _context.ProductVariants.FindAsync(request.VariantId);
        if (variant is null)
            return (false, "Phiên bản sản phẩm không tồn tại.", null);

        var item = _mapper.Map<OrderItem>(request);
        item.Price = (decimal)variant.Price;
        await _repository.AddAsync(item);
        await RecalculateOrderTotalAsync(item.OrderId);

        var createdItem = await _repository.GetByIdWithDetailsAsync(item.Id);
        return (true, null, _mapper.Map<OrderItemDTO>(createdItem));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "Số lượng phải lớn hơn 0.");

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return (false, "Không tìm thấy chi tiết đơn hàng.");

        var variant = await _context.ProductVariants.FindAsync(request.VariantId);
        if (variant is null)
            return (false, "Phiên bản sản phẩm không tồn tại.");

        _mapper.Map(request, item);
        item.Price = (decimal)variant.Price;
        await _repository.UpdateAsync(item);
        await RecalculateOrderTotalAsync(item.OrderId);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return false;

        var orderId = item.OrderId;
        await _repository.DeleteAsync(item);
        await RecalculateOrderTotalAsync(orderId);
        return true;
    }

    private async Task RecalculateOrderTotalAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order is null)
            return;

        order.TotalAmount = await _context.OrderItems
            .Where(item => item.OrderId == orderId)
            .SumAsync(item => (decimal?)(item.Price * item.Quantity)) ?? 0m;
        await _context.SaveChangesAsync();
    }
}
