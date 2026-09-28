using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IEnumerable<Review>> GetByProductIdAsync(int productId);
        Task<IEnumerable<Review>> GetByCustomerIdAsync(int customerId);
        Task<Review?> GetByCustomerAndProductAsync(int customerId, int productId);
        Task<bool> CustomerExistsAsync(int customerId);
        Task<bool> ProductExistsAsync(int productId);
    }
}
