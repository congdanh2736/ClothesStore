using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException khi chặn xóa khách hàng
using ClothesStore.Api.DTOs.Customer;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly IMembershipTierRepository _tierRepository; // [THÊM] để gán hạng mặc định cho khách mới
        private readonly IMapper _mapper;

        // [SỬA] thêm tham số IMembershipTierRepository
        public CustomerService(ICustomerRepository repository, IMembershipTierRepository tierRepository, IMapper mapper)
        {
            _repository = repository;
            _tierRepository = tierRepository; // [THÊM]
            _mapper = mapper;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var customers = await _repository.GetAllAsync();
            return _mapper.Map<List<CustomerDto>>(customers);
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var customer = await _repository.GetByIdWithDetailsAsync(id);
            return customer is null ? null : _mapper.Map<CustomerDto>(customer);
        }

        public async Task<(bool Success, string? Error, CustomerDto? Data)> CreateAsync(CreateCustomerDto dto)
        {
            // Business rule 1: ApplicationUser phải tồn tại
            if (!await _repository.ApplicationUserExistsAsync(dto.ApplicationUserId))
                return (false, "Tài khoản người dùng không tồn tại.", null);

            // [THÊM] 1 tài khoản chỉ gắn với 1 hồ sơ khách hàng (quan hệ 1-1, tránh lỗi trùng unique index)
            if (await _repository.GetByApplicationUserIdAsync(dto.ApplicationUserId) is not null)
                return (false, "Tài khoản này đã có hồ sơ khách hàng.", null);

            // Business rule 2: nếu có chọn MembershipTier thì tier đó phải tồn tại
            if (dto.MembershipTierId.HasValue &&
                !await _repository.MembershipTierExistsAsync(dto.MembershipTierId.Value))
                return (false, "Hạng thành viên không tồn tại.", null);

            var customer = _mapper.Map<Customer>(dto);
            // [THÊM] Khách mới chưa chọn hạng -> gán hạng thấp nhất (MinPoints = 0)
            customer.MembershipTierId ??= (await _tierRepository.GetTierForPointsAsync(0))?.Id;
            customer.Cart = new Cart(); // business rule: mỗi Customer mới luôn có sẵn 1 Cart rỗng

            await _repository.AddAsync(customer);

            // [SỬA] Load lại kèm MembershipTier để DTO trả về có tên hạng
            var created = await _repository.GetByIdWithDetailsAsync(customer.Id);
            return (true, null, _mapper.Map<CustomerDto>(created));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer is null) return (false, "Không tìm thấy khách hàng.");

            if (dto.MembershipTierId.HasValue && !await _repository.MembershipTierExistsAsync(dto.MembershipTierId.Value))
                return (false, "Hạng thành viên không tồn tại.");

            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            // [SỬA] Chỉ đổi hạng khi client gửi lên; trước đây không gửi thì hạng bị xóa về null.
            //       (Hạng còn được hệ thống tự xét theo điểm ở LoyaltyTransactionService.RecalculateTierAsync)
            if (dto.MembershipTierId.HasValue)
                customer.MembershipTierId = dto.MembershipTierId;

            await _repository.UpdateAsync(customer);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer is null) return false;

            // [THÊM] Khách đã có đơn hàng thì không xóa (Order -> Customer để Restrict, trước đây lỗi 500)
            if (await _repository.HasOrdersAsync(id))
                throw new BusinessException("Khách hàng đã có đơn hàng, không thể xóa.");

            await _repository.DeleteAsync(customer);
            return true;
        }
    }
}
