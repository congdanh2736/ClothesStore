using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException khi chặn xóa hạng
using ClothesStore.Api.DTOs.MembershipTier;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class MembershipTierService : IMembershipTierService
    {
        private readonly IMembershipTierRepository _repository;
        private readonly ILoyaltyTransactionService _loyaltyService; // [THÊM] để xét lại hạng khách khi đổi mốc điểm
        private readonly IMapper _mapper;

        // [SỬA] thêm tham số ILoyaltyTransactionService
        public MembershipTierService(IMembershipTierRepository repository, ILoyaltyTransactionService loyaltyService, IMapper mapper)
        {
            _repository = repository;
            _loyaltyService = loyaltyService; // [THÊM]
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
            // [THÊM] Mỗi hạng phải có mốc điểm tối thiểu khác nhau
            if (await _repository.MinPointsExistsAsync(dto.MinPoints))
                return (false, "Đã có hạng thành viên với cùng mức điểm tối thiểu.", null);

            var membershipTier = _mapper.Map<MembershipTier>(dto);
            await _repository.AddAsync(membershipTier);

            // [THÊM] Thêm hạng mới -> xét lại hạng cho toàn bộ khách hàng theo điểm hiện có
            await _loyaltyService.RecalculateAllTiersAsync();

            return (true, null, _mapper.Map<MembershipTierDto>(membershipTier));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateMembershipTierDto dto)
        {
            var membershipTier = await _repository.GetByIdAsync(id);
            // [SỬA] thông báo lỗi tiếng Việt cho đồng bộ
            if (membershipTier == null) return (false, "Không tìm thấy hạng thành viên.");

            // [THÊM] Không cho trùng mốc điểm với hạng khác
            if (await _repository.MinPointsExistsAsync(dto.MinPoints, id))
                return (false, "Đã có hạng thành viên với cùng mức điểm tối thiểu.");

            var minPointsChanged = membershipTier.MinPoints != dto.MinPoints; // [THÊM]
            membershipTier.TierName = dto.TierName;
            membershipTier.MinPoints = dto.MinPoints; // [THÊM] cập nhật mốc điểm
            await _repository.UpdateAsync(membershipTier);

            // [THÊM] Đổi mốc điểm -> xét lại hạng cho toàn bộ khách hàng
            if (minPointsChanged)
                await _loyaltyService.RecalculateAllTiersAsync();

            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // [SỬA] GetByIdAsync -> GetByIdWithDetailsAsync để lấy danh sách khách thuộc hạng
            var membershipTier = await _repository.GetByIdWithDetailsAsync(id);
            if (membershipTier == null) return false;

            // [THÊM] Hạng đang có khách thì không xóa (Customer -> MembershipTier để Restrict, trước đây lỗi 500)
            if (membershipTier.Customers.Any())
                throw new BusinessException("Hạng thành viên đang có khách hàng, không thể xóa.");

            await _repository.DeleteAsync(membershipTier);
            return true;
        }
    }
}
