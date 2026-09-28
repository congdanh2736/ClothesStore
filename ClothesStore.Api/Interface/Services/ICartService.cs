using ClothesStore.Api.DTOs.Cart;

namespace ClothesStore.Api.Interface.Services
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartByCustomerAsync(int customerId);
        Task<CartResponseDto> AddItemToCartAsync(AddToCartRequestDto request);
        Task<CartResponseDto> UpdateCartItemQuantityAsync(UpdateCartItemRequestDto request);
        Task<bool> RemoveItemFromCartAsync(int cartItemId);
    }
}