using ClothesStore.Api.DTOs.Address;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IAddressService : IService<AddressDto, CreateAddressDto, UpdateAddressDto>
    {
        // [THÊM] Sổ địa chỉ của 1 khách
        Task<IEnumerable<AddressDto>> GetByCustomerAsync(int customerId);
    }
}
