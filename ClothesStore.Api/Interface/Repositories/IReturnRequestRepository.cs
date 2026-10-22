using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IReturnRequestRepository : IRepository<ReturnRequest>
    {
        Task<IEnumerable<ReturnRequest>> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<ReturnRequest>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<ReturnRequest>> GetByOrderIdAndCustomerIdAsync(int orderId, int customerId);
        Task<ReturnRequest?> GetByIdAndCustomerIdAsync(int id, int customerId);
        // Tổng số lượng đã/đang trả (không tính yêu cầu bị từ chối) của từng OrderItem trong đơn
        Task<Dictionary<int, int>> GetReturnedQuantitiesAsync(int orderId, int? excludeReturnRequestId = null);
    }
}
