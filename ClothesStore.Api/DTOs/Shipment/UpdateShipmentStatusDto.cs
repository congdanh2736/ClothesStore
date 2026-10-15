// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO cập nhật trạng thái vận chuyển
namespace ClothesStore.Api.DTOs.Shipment
{
    public class UpdateShipmentStatusDto
    {
        public string Status { get; set; } = string.Empty;   // Shipping | Delivered | Failed
    }
}
