namespace ClothesStore.Api.DTOs.ProductImage
{
    public class ProductImageDto {
        public int ImageId {get; set;}
        public string? ImageUrl { get; set; }
        public int ProductId { get; set; }
    }
}