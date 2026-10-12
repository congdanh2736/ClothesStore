namespace ClothesStore.Api.DTOs.CartItem
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        // Thông tin chi tiết từ ProductVariant & Product
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double Price { get; set; } | public double TotalPrice => Price * Quantity;)
        public decimal Price { get; set; }
        public decimal TotalPrice => Price * Quantity;
    }
}
