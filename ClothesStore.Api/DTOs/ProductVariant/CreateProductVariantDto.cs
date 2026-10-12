namespace ClothesStore.Api.DTOs.ProductVariant
{
    public class CreateProductVariantDto
    {
        public string? Color { get; set; }
        public string? Size { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double Price { get; set; })
        public decimal Price { get; set; }
        public int ProductId { get; set; }
    }
}
