using AutoMapper;
using ClothesStore.Api.DTOs.ProductVariant;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ProductVariantProfile : Profile {
        public ProductVariantProfile()
        {
            CreateMap<ProductVariant,ProductVariantDto>();
            CreateMap<CreateProductVariantDto, ProductVariant>();
        }
    }
}