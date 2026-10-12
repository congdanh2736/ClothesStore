namespace ClothesStore.Api.DTOs.ProductVariant
{
    public class ProductVariantDto {
        public int Id {get;set;}
        // [THÊM] Trả kèm ProductId
        public int ProductId { get; set; }
        public string? Color {get;set;}
        public string? Size { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double Price { get; set; })
        public decimal Price { get; set; }
        public string? ProductName { get; set; }
    }
}
