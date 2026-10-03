namespace ClothesStore.Api.DTOs.SizeChart
{
    public class SizeChartDto {
        public int Id {get;set;}
        public string? SizeLabel { get; set; }
        public string? Measurements { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}