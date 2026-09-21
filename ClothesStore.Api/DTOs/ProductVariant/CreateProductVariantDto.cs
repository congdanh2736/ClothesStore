namespace ClothesStore.Api.DTOs.ProductVariant
{
    public class CreateProductVariantDto
    {
        public string? Color { get; set; }
        public string? Size { get; set; }
        public double Price { get; set; }
        public int ProductId { get; set; }
    }
}