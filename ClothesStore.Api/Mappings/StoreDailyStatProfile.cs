using AutoMapper;
using ClothesStore.Api.DTOs.StoreDailyStat;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class StoreDailyStatProfile : Profile
    {
        public StoreDailyStatProfile()
        {
            CreateMap<StoreDailyStat, StoreDailyStatDto>();
            CreateMap<CreateStoreDailyStatDto, StoreDailyStat>();
            CreateMap<UpdateStoreDailyStatDto, StoreDailyStat>();
        }
    }
}
