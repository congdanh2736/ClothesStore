namespace ClothesStore.Api.DTOs.Review
{
    public class CreateReviewDto
    {
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
