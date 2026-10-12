// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
namespace ClothesStore.Api.DTOs.PaymentTransaction
{
    public class PaymentTransactionDto
    {
        public int Id { get; set; }
        // [THÊM] Trả thêm OrderId, trạng thái, mã giao dịch, ngày thanh toán, tổng tiền đơn
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        // [THÊM] ===== BẮT ĐẦU =====
        public string? Status { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal OrderTotal { get; set; }
        // [THÊM] ===== KẾT THÚC =====
    }
}
