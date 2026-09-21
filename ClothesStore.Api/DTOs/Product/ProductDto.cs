namespace ClothesStore.Api.DTOs.Product
{
    public class ProductDto {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CategorName { get; set; } = string.Empty;
        public string? MainProductImageUrl { get; set; } = string.Empty;
        
    }
}