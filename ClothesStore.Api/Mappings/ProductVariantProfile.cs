using AutoMapper;
using ClothesStore.Api.DTOs.ProductVariant;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ProductVariantProfile : Profile {
        public ProductVariantProfile()
        {
            CreateMap<ProductVariant, ProductVariantDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));

            CreateMap<CreateProductVariantDto, ProductVariant>();
            CreateMap<UpdateProductVariantDto, ProductVariant>();
        }
    }
}