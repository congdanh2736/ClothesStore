using AutoMapper;
using ClothesStore.Api.DTOs.SizeChart;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class SizeChartProfile : Profile{
        public SizeChartProfile()
        {
            CreateMap<SizeChart, SizeChartDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));
            CreateMap<CreateSizeChartDto, SizeChart>();
            CreateMap<UpdateSizeChartDto, SizeChart>();
        }
    }
}