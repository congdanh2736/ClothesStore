// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;

namespace ClothesStore.Api.DTOs.Order
{
    // Chi tiet don hang
    public class OrderDetailDTO
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } // [THÊM] ngày đặt hàng
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AddressId { get; set; }
        public string Address { get; set; } = string.Empty;
        public int? PromotionId { get; set; }   // [SỬA] int -> int? (khuyến mãi không bắt buộc)
        public string? PromotionCode { get; set; } // [THÊM] mã khuyến mãi đã áp dụng
        public int? StoreId { get; set; }          // [THÊM] cửa hàng xuất kho
        public string? StoreName { get; set; }     // [THÊM]
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }    // [THÊM] trạng thái thanh toán
        public string? ShipmentStatus { get; set; }   // [THÊM] trạng thái vận chuyển
        public string? TrackingNumber { get; set; }   // [THÊM] mã vận đơn
        public decimal Subtotal { get; set; }         // [THÊM] tổng tiền hàng trước giảm giá
        public decimal DiscountAmount { get; set; }   // [THÊM] số tiền được giảm
        public decimal TotalAmount { get; set; }      // số tiền phải trả (= Subtotal - DiscountAmount)
        public List<OrderItemDTO> OrderItems { get; set; } = new();
    }
}
