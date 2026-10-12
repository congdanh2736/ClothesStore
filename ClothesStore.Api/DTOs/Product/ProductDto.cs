namespace ClothesStore.Api.DTOs.Product
{
    public class ProductDto {
        public int Id { get; set; }
        public string? Name { get; set;}
        // [THÊM] Trả kèm CategoryId
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; } = string.Empty;
        public string? MainProductImageUrl { get; set; } = string.Empty;
        // [THÊM] Giá thấp nhất trong các biến thể để hiển thị danh sách
        public decimal? MinPrice { get; set; }     // giá thấp nhất trong các biến thể
    }
}
