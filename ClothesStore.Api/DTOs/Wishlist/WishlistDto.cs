namespace ClothesStore.Api.DTOs.Wishlist
{
    public class WishlistDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        // [SỬA] Đổi double -> decimal cho đồng bộ kiểu tiền với ProductVariant.Price / OrderItem.Price (trước đây: public double Price { get; set; })
        public decimal Price { get; set; }       // giá thấp nhất trong các biến thể
        public bool InStock { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
