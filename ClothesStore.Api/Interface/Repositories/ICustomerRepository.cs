using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<bool> ApplicationUserExistsAsync(string applicationUserId);
        Task<bool> MembershipTierExistsAsync(int tierId);
    }
}
