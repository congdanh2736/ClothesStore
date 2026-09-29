using AutoMapper;
using ClothesStore.Api.DTOs.ProductImage;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ProductImageProfile : Profile {
        public ProductImageProfile()
        {
            CreateMap<ProductImage, ProductImageDto>();
            CreateMap<CreateProductImageDto, ProductImage>();
        }
    }
}