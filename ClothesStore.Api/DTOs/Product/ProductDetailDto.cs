namespace ClothesStore.Api.DTOs.Product
{
    public class ProductDetailDto {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CategorName { get; set; } = string.Empty;
        public string[]? ProductImageUrl { get; set; }
        public string[]? ProductVariant { get; set; }
        public string[]? ProductCollection { get; set; }
    }
}