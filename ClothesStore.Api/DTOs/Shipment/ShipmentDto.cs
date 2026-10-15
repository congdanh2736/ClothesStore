// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO trả về của bảng Shipment
namespace ClothesStore.Api.DTOs.Shipment
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OrderStatus { get; set; }
    }
}
