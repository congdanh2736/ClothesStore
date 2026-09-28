using AutoMapper;
using ClothesStore.Api.DTOs.Address;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _repository;
        private readonly IMapper _mapper;

        public AddressService(
            IAddressRepository repository,
            IMapper mapper
        )
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Lấy tất cả địa chỉ
        public async Task<IEnumerable<AddressDto>> GetAllAsync()
        {
            var addresses = await _repository.GetAllAsync();
            return _mapper.Map<List<AddressDto>>(addresses);
        }

        // Lấy địa chỉ theo ID
        public async Task<AddressDto?> GetByIdAsync(int id)
        {
            var address = await _repository.GetByIdWithDetailsAsync(id);
            return address is null ? null : _mapper.Map<AddressDto>(address);
        }

        // Tạo địa chỉ mới
        public async Task<(bool Success, string? Error, AddressDto? Data)> CreateAsync(CreateAddressDto dto)
        {
            // Kiểm tra xem khách hàng có tồn tại không
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Customer not found", null);

            // Tạo địa chỉ mới và lưu vào cơ sở dữ liệu
            var address = _mapper.Map<Address>(dto);
            await _repository.AddAsync(address);
            return (true, null, _mapper.Map<AddressDto>(address));
        }

        // Cập nhật địa chỉ
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateAddressDto dto)
        {
            // Kiểm tra xem địa chỉ có tồn tại không
            var address = await _repository.GetByIdAsync(id);
            if (address is null) return (false, "Address not found");

            // Kiểm tra xem khách hàng có tồn tại không
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Customer not found");

            // Cập nhật thông tin địa chỉ và lưu vào cơ sở dữ liệu
            address.Street = dto.Street;
            address.District = dto.District;
            address.City = dto.City;
            address.Country = dto.Country;
            address.CustomerId = dto.CustomerId;

            // Lưu thay đổi vào cơ sở dữ liệu
            await _repository.UpdateAsync(address);
            return (true, null);
        }

        // Xóa địa chỉ
        public async Task<bool> DeleteAsync(int id)
        {
            // Kiểm tra xem địa chỉ có tồn tại không
            var address = await _repository.GetByIdAsync(id);
            if (address is null) return false;

            // Xóa địa chỉ khỏi cơ sở dữ liệu
            await _repository.DeleteAsync(address);
            return true;
        }
    }
}
