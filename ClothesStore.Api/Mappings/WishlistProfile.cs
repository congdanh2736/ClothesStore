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
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));

            CreateMap<CreateWishlistDto, Wishlist>();
        }
    }
}
