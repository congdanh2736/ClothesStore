// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO tạo vận đơn
namespace ClothesStore.Api.DTOs.Shipment
{
    // Tạo vận đơn cho đơn hàng đã được xác nhận -> đơn chuyển sang Shipping
    public class CreateShipmentDto
    {
        public int OrderId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
    }
}
