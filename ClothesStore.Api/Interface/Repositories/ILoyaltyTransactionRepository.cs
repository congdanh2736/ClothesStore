using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ILoyaltyTransactionRepository : IRepository<LoyaltyTransaction>
    {
        Task<IEnumerable<LoyaltyTransaction>> GetByCustomerIdAsync(int customerId);
        // Tổng điểm hiện tại của khách = tổng PointsChange của các giao dịch
        Task<int> GetTotalPointsAsync(int customerId);
        Task<bool> CustomerExistsAsync(int customerId);
    }
}
