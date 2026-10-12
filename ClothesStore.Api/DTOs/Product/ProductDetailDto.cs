using ClothesStore.Api.DTOs.CollectionTech; // [THÊM]
using ClothesStore.Api.DTOs.ProductVariant; // [THÊM]

namespace ClothesStore.Api.DTOs.Product
{
    // [SỬA] Chi tiết sản phẩm: ảnh, các biến thể (màu/size/giá), bộ sưu tập - công nghệ
    public class ProductDetailDto {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CategoryId { get; set; } // [THÊM]
        public string? CategoryName { get; set; } = string.Empty;
        // [SỬA] (trước đây: string[]? ProductImageUrl, string[]? ProductVariant ("màu - size", không có giá),
        //        string[]? ProductCollection) -> đổi sang danh sách có cấu trúc để client dùng được Id/giá
        public List<string> ImageUrls { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<CollectionTechDto> Collections { get; set; } = new();
    }
}
