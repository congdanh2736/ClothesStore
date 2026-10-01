using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services;

public interface IOrderItemService : IService<OrderItemDTO, CreateOrderItemRequest, UpdateOrderItemRequest>
{
}
