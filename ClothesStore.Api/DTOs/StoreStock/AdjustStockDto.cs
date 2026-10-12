// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO nhập / xuất kho thủ công (POST /api/StoreStock/adjust)
namespace ClothesStore.Api.DTOs.StoreStock
{
    // Nhập / xuất kho thủ công: QuantityChange > 0 là nhập, < 0 là xuất (kiểm kê, hàng lỗi...)
    public class AdjustStockDto
    {
        public int StoreId { get; set; }
        public int VariantId { get; set; }
        public int QuantityChange { get; set; }
    }
}
