namespace ClothesStore.Api.DTOs.CartItem
{
    public class CreateCartItemDto
    {
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
