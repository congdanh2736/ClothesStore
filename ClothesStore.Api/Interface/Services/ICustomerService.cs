using ClothesStore.Api.DTOs.Customer;

namespace ClothesStore.Api.Interface.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<(bool Success, string? Error, CustomerDto? Data)> CreateAsync(CreateCustomerDto dto);
        Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateCustomerDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
