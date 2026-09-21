using AutoMapper;
using ClothesStore.Api.DTOs.MembershipTier;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class MembershipTierProfile : Profile
    {
        public MembershipTierProfile()
        {
            CreateMap<MembershipTier, MembershipTierDto>()
                .ForMember(dest => dest.CustomerCount, 
                    opt => opt.MapFrom(src => src.Customers.Count));
            CreateMap<CreateMembershipTierDto, MembershipTier>();
        }
    }
}
