using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IEmployeeRepository : IRepository<Employee>
    {
        Task<bool> StoreExistsAsync(int? storeId);
    }
}
