using AutoMapper;
using ClothesStore.Api.DTOs.SizeChart;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class SizeChartProfile : Profile{
        public SizeChartProfile()
        {
            CreateMap<SizeChart,SizeChartDto>();
            CreateMap<CreateSizeChartDto, SizeChart>();
        }
    }
}