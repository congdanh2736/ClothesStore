using AutoMapper;
using ClothesStore.Api.DTOs.Wishlist;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class WishlistProfile : Profile
    {
        public WishlistProfile()
        {
            CreateMap<Wishlist, WishlistDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductImageUrl,
                    opt => opt.MapFrom(src => src.Product != null && src.Product.ProductImages != null
                        ? src.Product.ProductImages.FirstOrDefault()!.ImageUrl
                        : null))
                .ForMember(dest => dest.Price,
                    opt => opt.MapFrom(src => src.Product != null && src.Product.ProductVariants != null && src.Product.ProductVariants.Any()
                        ? src.Product.ProductVariants.Min(v => v.Price)
                        : 0))
                .ForMember(dest => dest.InStock,
                    opt => opt.MapFrom(src => src.Product != null && src.Product.ProductVariants != null && src.Product.ProductVariants.Any()));

            CreateMap<CreateWishlistDto, Wishlist>();
        }
    }
}
