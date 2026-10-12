namespace ClothesStore.Api.DTOs.StoreStock
{
    public class StoreStockDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        // [THÊM] Trả kèm tên cửa hàng
        public string? StoreName { get; set; }
        public int VariantId { get; set; }
        // [THÊM] Trả kèm tên sản phẩm, màu, size
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int Quantity { get; set; }
    }
}
