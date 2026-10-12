namespace ClothesStore.Api.DTOs.Cart
{
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double Price { get; set; })
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double TotalPrice => Price * Quantity;)
        public decimal TotalPrice => Price * Quantity;
    }
}
