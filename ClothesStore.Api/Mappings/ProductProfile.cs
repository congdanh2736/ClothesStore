using AutoMapper;
using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ProductProfile : Profile {
        public ProductProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(c => c.CategoryName,
                    s => s.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(c => c.MainProductImageUrl,
                    s => s.MapFrom(src => src.ProductImages.Select(x => x.ImageUrl).FirstOrDefault()));

            CreateMap<Product, ProductDetailDto>()
                .ForMember(c => c.CategoryName,
                    s => s.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(c => c.ProductImageUrl,
                    s => s.MapFrom(src => src.ProductImages.Select(x => x.ImageUrl).ToArray()))
                .ForMember(c => c.ProductVariant,
                    s => s.MapFrom(src => src.ProductVariants.Select(x => $"{x.Color} - {x.Size}").ToArray()))
                .ForMember(c => c.ProductCollection,
                    s => s.MapFrom(src => src.ProductCollections.Select(x => x.CollectionTech != null ? x.CollectionTech.Name : "").ToArray()));

            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}