using AutoMapper;
using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class StoreStockProfile : Profile
    {
        public StoreStockProfile()
        {
            CreateMap<StoreStock, StoreStockDto>();
            CreateMap<CreateStoreStockDto, StoreStock>();
            CreateMap<UpdateStoreStockDto, StoreStock>();
        }
    }
}
