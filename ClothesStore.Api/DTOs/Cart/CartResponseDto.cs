namespace ClothesStore.Api.DTOs.Cart
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new();
    }
}