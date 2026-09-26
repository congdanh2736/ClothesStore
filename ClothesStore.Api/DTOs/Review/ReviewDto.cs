namespace ClothesStore.Api.DTOs.Review
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
