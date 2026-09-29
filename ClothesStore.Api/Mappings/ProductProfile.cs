using AutoMapper;
using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ProductProfile : Profile {
        public ProductProfile()
        {
            CreateMap<Product,ProductDto>()
                .ForMember(c => c.CategoryName,
                    s => s.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(c => c.MainProductImageUrl,
                    s => s.MapFrom(src => src.ProductImages.First()));
            CreateMap<CreateProductDto, Product>();
        }
    }
}