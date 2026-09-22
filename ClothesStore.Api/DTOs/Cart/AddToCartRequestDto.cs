namespace ClothesStore.DTOs.Cart
{
    public class AddToCartRequestDto
    {
        public int CustomerId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}