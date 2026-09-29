using ClothesStore.Api.DTOs.CollectionTech;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface ICollectionTechService : IService<CollectionTechDto,CreateCollectionTechDto,UpdateCollectionTechDto>  {
        
    }
}