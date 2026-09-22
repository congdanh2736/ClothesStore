using ClothesStore.DTOs.Cart;

namespace ClothesStore.Services
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartByCustomerAsync(int customerId);
        Task<CartResponseDto> AddItemToCartAsync(AddToCartRequestDto request);
        Task<CartResponseDto> UpdateCartItemQuantityAsync(UpdateCartItemRequestDto request);
        Task<bool> RemoveItemFromCartAsync(int cartItemId);
    }
}