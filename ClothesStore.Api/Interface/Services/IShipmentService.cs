// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Interface service bảng Shipment (bảng 23 trong ERD - trước đây chưa có)
using ClothesStore.Api.DTOs.Shipment;

namespace ClothesStore.Api.Interface.Services
{
    public interface IShipmentService
    {
        Task<IEnumerable<ShipmentDto>> GetAllAsync();
        Task<ShipmentDto?> GetByIdAsync(int id);
        Task<ShipmentDto?> GetByOrderIdAsync(int orderId);

        // Tạo vận đơn cho đơn đã xác nhận -> đơn chuyển sang Shipping
        Task<ShipmentDto> CreateAsync(CreateShipmentDto dto);
        Task<ShipmentDto> UpdateAsync(int id, UpdateShipmentDto dto);

        // Delivered -> hoàn tất đơn; Failed -> giao thất bại (có thể giao lại hoặc hủy đơn)
        Task<ShipmentDto> UpdateStatusAsync(int id, UpdateShipmentStatusDto dto);
    }
}
