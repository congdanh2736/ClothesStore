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
                // [THÊM] Tổng điểm = SUM(PointsChange) của Loyalty_Transaction
                .ForMember(dest => dest.TotalPoints,
                    opt => opt.MapFrom(src => src.LoyaltyTransactions.Sum(t => t.PointsChange)))
                .ForMember(dest => dest.AddressCount,
                    opt => opt.MapFrom(src => src.Addresses.Count))
                .ForMember(dest => dest.WishlistCount,
                    opt => opt.MapFrom(src => src.Wishlists.Count))
                // [THÊM] Email lấy từ tài khoản đăng nhập (FirstName, LastName tự map vì trùng tên)
                .ForMember(dest => dest.Email,
                    opt => opt.MapFrom(src => src.ApplicationUser != null ? src.ApplicationUser.Email : null));

            CreateMap<CreateCustomerDto, Customer>();
        }
    }
}
