// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO ghi nhận kết quả thanh toán (PATCH /api/PaymentTransaction/{id}/status)
namespace ClothesStore.Api.DTOs.PaymentTransaction
{
    // Cập nhật kết quả thanh toán (callback cổng thanh toán / nhân viên xác nhận đã nhận tiền)
    public class UpdatePaymentStatusRequest
    {
        public string Status { get; set; } = string.Empty;   // Paid | Failed | Pending
        public string? TransactionCode { get; set; }
    }
}
