namespace ClothesStore.Api.DTOs.ProductVariant
{
    public class ProductVariantDto {
        public int Id {get;set;}
        public string? Color {get;set;}
        public string? Size { get; set; }
        public double Price { get; set; }
        public string? ProductName { get; set; }
    }
}