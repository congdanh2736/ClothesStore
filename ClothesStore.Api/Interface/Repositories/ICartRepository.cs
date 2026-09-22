using ClothesStore.Models;

namespace ClothesStore.Repositories
{
    public interface ICartRepository
    {
        Task<Cart> GetCartByCustomerIdAsync(int customerId);
        Task<CartItem> GetCartItemByIdAsync(int cartItemId);
        Task<CartItem> GetCartItemByVariantAsync(int cartId, int variantId);
        Task AddCartAsync(Cart cart);
        Task AddCartItemAsync(CartItem cartItem);
        void RemoveCartItem(CartItem cartItem);
        Task<int> SaveChangesAsync();
    }
}