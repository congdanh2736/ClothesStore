namespace ClothesStore.Api.DTOs.Cart
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;)
        public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;
        public List<CartItemResponseDto> Items { get; set; } = new();
    }
}
