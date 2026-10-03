namespace ClothesStore.Api.DTOs.ProductImage
{
    public class CreateProductImageDto {
        public string? ImageUrl { get; set; }
        public int ProductId { get; set; }
    }
}