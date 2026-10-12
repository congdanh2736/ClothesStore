using AutoMapper;
using ClothesStore.Api.DTOs.CartItem;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class CartItemProfile : Profile
    {
        public CartItemProfile()
        {
            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.ProductVariant != null && src.ProductVariant.Product != null
                        ? src.ProductVariant.Product.Name
                        : null))
                .ForMember(dest => dest.Color,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Color : null))
                .ForMember(dest => dest.Size,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Size : null))
                .ForMember(dest => dest.Price,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Price : 0));

            CreateMap<CreateCartItemDto, CartItem>();
            CreateMap<UpdateCartItemDto, CartItem>();
        }
    }
}
