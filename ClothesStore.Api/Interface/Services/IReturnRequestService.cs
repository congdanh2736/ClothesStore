// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Interface service đổi/trả hàng: Return_Request + Return_Item (bảng 17, 18 - trước đây chưa có)
using ClothesStore.Api.DTOs.ReturnRequest;

namespace ClothesStore.Api.Interface.Services
{
    public interface IReturnRequestService
    {
        Task<IEnumerable<ReturnRequestDto>> GetAllAsync();
        Task<ReturnRequestDto?> GetByIdAsync(int id, string userId = "", bool isStaff = true);
        Task<IEnumerable<ReturnRequestDto>> GetByOrderIdAsync(int orderId, string userId = "", bool isStaff = true);
        Task<IEnumerable<ReturnRequestDto>> GetByCustomerIdAsync(int customerId, string userId = "", bool isStaff = true);

        Task<ReturnRequestDto> CreateAsync(CreateReturnRequestDto dto, string userId);
        Task<ReturnRequestDto> ApproveAsync(int id);
        Task<ReturnRequestDto> RejectAsync(int id);
        // Đã nhận lại hàng: hoàn kho, trừ điểm, cập nhật thanh toán / thống kê
        Task<ReturnRequestDto> CompleteAsync(int id);
        Task<bool> DeleteAsync(int id, string userId = "", bool isStaff = true);
    }
}
