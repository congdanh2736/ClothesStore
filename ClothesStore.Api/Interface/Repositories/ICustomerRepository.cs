using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByIdWithDetailsAsync(int id); // kèm MembershipTier, Addresses
        Task<bool> ApplicationUserExistsAsync(string applicationUserId);
        Task<bool> MembershipTierExistsAsync(int tierId);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Customer customer);
    }
}
