using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ICartItemRepository : IRepository<CartItem>
    {
        Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId);
        Task<CartItem?> GetByCartAndVariantAsync(int cartId, int variantId);
        Task<bool> CartExistsAsync(int cartId);
        Task<bool> VariantExistsAsync(int variantId);
    }
}
