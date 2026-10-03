using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IStoreStockService : IService<StoreStockDto, CreateStoreStockDto, UpdateStoreStockDto>
    {
    }
}
