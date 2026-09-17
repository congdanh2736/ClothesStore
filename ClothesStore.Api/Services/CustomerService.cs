using AutoMapper;
using ClothesStore.Api.DTOs.Customer;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly IMapper _mapper;

        public CustomerService(ICustomerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<CustomerDto>> GetAllAsync()
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

            // Business rule 2: nếu có chọn MembershipTier thì tier đó phải tồn tại
            if (dto.MembershipTierId.HasValue &&
                !await _repository.MembershipTierExistsAsync(dto.MembershipTierId.Value))
                return (false, "Hạng thành viên không tồn tại.", null);

            var customer = _mapper.Map<Customer>(dto);
            customer.Cart = new Cart(); // business rule: mỗi Customer mới luôn có sẵn 1 Cart rỗng

            await _repository.AddAsync(customer);
            return (true, null, _mapper.Map<CustomerDto>(customer));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer is null) return (false, "Không tìm thấy khách hàng.");

            if (dto.MembershipTierId.HasValue && !await _repository.MembershipTierExistsAsync(dto.MembershipTierId.Value))
                return (false, "Hạng thành viên không tồn tại.");

            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.MembershipTierId = dto.MembershipTierId;

            await _repository.UpdateAsync(customer);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer is null) return false;

            await _repository.DeleteAsync(customer);
            return true;
        }
    }
}
