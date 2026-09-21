using AutoMapper;
using ClothesStore.Api.DTOs.MembershipTier;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class MembershipTierService : IMembershipTierService
    {
        private readonly IMembershipTierRepository _repository;
        private readonly IMapper _mapper;

        public MembershipTierService(IMembershipTierRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipTierDto>> GetAllAsync()
        {
            var membershipTiers = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MembershipTierDto>>(membershipTiers);
        }

        public async Task<MembershipTierDto?> GetByIdAsync(int id)
        {
            var membershipTier = await _repository.GetByIdWithDetailsAsync(id);
            return membershipTier == null ? null : _mapper.Map<MembershipTierDto>(membershipTier);
        }

        public async Task<(bool Success, string? Error, MembershipTierDto? Data)> CreateAsync(CreateMembershipTierDto dto)
        {
            var membershipTier = _mapper.Map<MembershipTier>(dto);
            await _repository.AddAsync(membershipTier);
            return (true, null, _mapper.Map<MembershipTierDto>(membershipTier));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateMembershipTierDto dto)
        {
            var membershipTier = await _repository.GetByIdAsync(id);
            if (membershipTier == null) return (false, "Can't find tier!");
            membershipTier.TierName = dto.TierName;
            await _repository.UpdateAsync(membershipTier);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var membershipTier = await _repository.GetByIdAsync(id);
            if (membershipTier == null) return false;
            await _repository.DeleteAsync(membershipTier);
            return true;
        }
    }
}
