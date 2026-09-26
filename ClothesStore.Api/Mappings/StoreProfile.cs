using AutoMapper;
using ClothesStore.Api.DTOs.Store;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class StoreProfile : Profile
    {
        public StoreProfile()
        {
            CreateMap<Store, StoreDto>();
            CreateMap<CreateStoreDto, Store>();
            CreateMap<UpdateStoreDto, Store>();
        }
    }
}
