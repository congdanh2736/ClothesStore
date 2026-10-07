namespace ClothesStore.Api.DTOs.Cart
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
        public double TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;
        public List<CartItemResponseDto> Items { get; set; } = new();
    }
}