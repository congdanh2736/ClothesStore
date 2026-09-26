namespace ClothesStore.Api.DTOs.Wishlist
{
    public class WishlistDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
