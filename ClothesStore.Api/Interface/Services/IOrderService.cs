// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IOrderService : IService<OrderDetailDTO, CreateOrderRequest, UpdateOrderRequest> 
    {
        // [THÊM] Lịch sử đơn hàng của 1 khách
        Task<IEnumerable<OrderDetailDTO>> GetByCustomerIdAsync(int customerId);

        // [THÊM] Các bước của vòng đời đơn hàng ===== BẮT ĐẦU =====
        // [THÊM] Đặt hàng từ giỏ hàng: tạo đơn + trừ kho + tạo giao dịch thanh toán + xóa giỏ
        Task<OrderDetailDTO> CheckoutAsync(CheckoutRequest request);

        // [THÊM] Pending -> Confirmed
        Task<OrderDetailDTO> ConfirmAsync(int id);

        // [THÊM] Pending/Confirmed -> Cancelled (hoàn kho, hủy/hoàn tiền thanh toán)
        Task<OrderDetailDTO> CancelAsync(int id);

        // [THÊM] Confirmed (khách lấy tại cửa hàng) / Shipping (giao thành công) -> Completed
        // [THÊM] (thanh toán COD, cộng điểm, cập nhật thống kê cửa hàng)
        Task<OrderDetailDTO> CompleteAsync(int id);
        // [THÊM] ===== KẾT THÚC =====
    }
}
