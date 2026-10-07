namespace ClothesStore.Api.DTOs.Wishlist
{
    public class WishlistDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public double Price { get; set; }
        public bool InStock { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
