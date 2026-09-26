using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IWishlistRepository : IRepository<Wishlist>
    {
        Task<IEnumerable<Wishlist>> GetByCustomerIdAsync(int customerId);
        Task<Wishlist?> GetByCustomerAndProductAsync(int customerId, int productId);
        Task<bool> CustomerExistsAsync(int customerId);
        Task<bool> ProductExistsAsync(int productId);
    }
}
