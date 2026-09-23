using ClothesStore.Api.DTOs.StoreItemStat;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IStoreItemStatService : IService<StoreItemStatDto, CreateStoreItemStatDto, UpdateStoreItemStatDto>
    {
    }
}
