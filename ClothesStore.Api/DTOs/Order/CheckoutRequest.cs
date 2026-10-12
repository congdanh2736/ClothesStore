// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO đặt hàng từ giỏ hàng (POST /api/Order/checkout)
namespace ClothesStore.Api.DTOs.Order
{
    // Đặt hàng từ giỏ hàng hiện tại của khách (luồng chính của website)
    public class CheckoutRequest
    {
        public int CustomerId { get; set; }
        public int AddressId { get; set; }
        public string? PromotionCode { get; set; }  // mã giảm giá khách nhập, ví dụ SALE10K
        public int? StoreId { get; set; }           // null = hệ thống tự chọn cửa hàng còn đủ hàng
        public string? PaymentMethod { get; set; }  // mặc định COD
    }
}
