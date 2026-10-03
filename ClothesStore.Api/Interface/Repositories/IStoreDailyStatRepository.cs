using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IStoreDailyStatRepository : IRepository<StoreDailyStat>
    {
        Task<bool> StoreExistsAsync(int storeId);
        Task<StoreDailyStat?> GetByStoreAndDateAsync(int storeId, DateOnly statDate);
    }
}
