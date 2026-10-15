using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IShipmentRepository : IRepository<Shipment>
    {
        Task<Shipment?> GetByOrderIdAsync(int orderId);
    }
}
