using AutoMapper;
using ClothesStore.Api.DTOs.StoreItemStat;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class StoreItemStatProfile : Profile
    {
        public StoreItemStatProfile()
        {
            CreateMap<StoreItemStat, StoreItemStatDto>();
            CreateMap<CreateStoreItemStatDto, StoreItemStat>();
            CreateMap<UpdateStoreItemStatDto, StoreItemStat>();
        }
    }
}
