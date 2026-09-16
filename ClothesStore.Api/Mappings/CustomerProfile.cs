using AutoMapper;
using ClothesStore.Api.DTOs.Customer;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customer, CustomerDto>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.MembershipTierName,
                    opt => opt.MapFrom(src => src.MembershipTier != null ? src.MembershipTier.TierName : null))
                .ForMember(dest => dest.AddressCount,
                    opt => opt.MapFrom(src => src.Addresses.Count))
                .ForMember(dest => dest.WishlistCount,
                    opt => opt.MapFrom(src => src.Wishlists.Count));

            CreateMap<CreateCustomerDto, Customer>();
        }
    }
}
