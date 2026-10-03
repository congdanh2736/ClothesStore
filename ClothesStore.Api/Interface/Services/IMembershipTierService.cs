using ClothesStore.Api.DTOs.MembershipTier;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IMembershipTierService : IService<MembershipTierDto, CreateMembershipTierDto, UpdateMembershipTierDto>
    {

    }
}
