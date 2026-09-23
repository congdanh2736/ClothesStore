using ClothesStore.Api.DTOs.Store;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IStoreService : IService<StoreDto, CreateStoreDto, UpdateStoreDto>
    {
    }
}
