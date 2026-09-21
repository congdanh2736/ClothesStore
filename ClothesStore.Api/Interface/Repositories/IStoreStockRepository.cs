using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IStoreStockRepository : IRepository<StoreStock>
    {
        Task<bool> StoreExistsAsync(int storeId);
        Task<bool> ProductVariantExistsAsync(int variantId);
        Task<StoreStock?> GetByStoreAndVariantAsync(int storeId, int variantId);
    }
}
