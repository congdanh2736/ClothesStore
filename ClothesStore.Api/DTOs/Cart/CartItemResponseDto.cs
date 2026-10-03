namespace ClothesStore.Api.DTOs.Cart
{
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}