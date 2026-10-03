using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IStoreItemStatRepository : IRepository<StoreItemStat>
    {
        Task<bool> StoreExistsAsync(int storeId);
        Task<bool> ProductVariantExistsAsync(int variantId);
        Task<StoreItemStat?> GetByStoreVariantAndDateAsync(int storeId, int variantId, DateOnly statDate);
    }
}
