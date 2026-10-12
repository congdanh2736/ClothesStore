using AutoMapper;
using ClothesStore.Api.DTOs.CollectionTech; // [THÊM] cho ProductDetailDto.Collections
using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.DTOs.ProductVariant; // [THÊM] cho ProductDetailDto.Variants
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
                    s => s.MapFrom(src => src.ProductImages.Select(x => x.ImageUrl).FirstOrDefault()))
                // [THÊM] giá thấp nhất trong các biến thể
                .ForMember(c => c.MinPrice,
                    s => s.MapFrom(src => src.ProductVariants.Any()
                        ? src.ProductVariants.Min(v => v.Price)
                        : (decimal?)null));

            CreateMap<Product, ProductDetailDto>()
                .ForMember(c => c.CategoryName,
                    s => s.MapFrom(src => src.Category != null ? src.Category.Name : null))
                // [SỬA] ===== BẮT ĐẦU: map theo cấu trúc mới của ProductDetailDto =====
                // (trước đây: ProductImageUrl = string[], ProductVariant = string[] "màu - size" (không có giá),
                //  ProductCollection = string[] tên bộ sưu tập)
                .ForMember(c => c.ImageUrls,
                    s => s.MapFrom(src => src.ProductImages
                        .Where(x => x.ImageUrl != null)
                        .Select(x => x.ImageUrl!)
                        .ToList()))
                .ForMember(c => c.Variants,
                    s => s.MapFrom(src => src.ProductVariants))
                .ForMember(c => c.Collections,
                    s => s.MapFrom(src => src.ProductCollections
                        .Where(pc => pc.CollectionTech != null)
                        .Select(pc => pc.CollectionTech!)));
                // [SỬA] ===== KẾT THÚC =====

            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
