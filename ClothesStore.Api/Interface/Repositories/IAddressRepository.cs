using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IAddressRepository : IRepository<Address>
    {
        // Kiểm tra xem Customer có tồn tại hay không dựa trên customerId 
        public Task<bool> CustomerExistsAsync(int? customerId);
    }
}
