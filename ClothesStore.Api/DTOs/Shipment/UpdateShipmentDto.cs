// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO sửa thông tin vận đơn
namespace ClothesStore.Api.DTOs.Shipment
{
    public class UpdateShipmentDto
    {
        public string Carrier { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
    }
}
