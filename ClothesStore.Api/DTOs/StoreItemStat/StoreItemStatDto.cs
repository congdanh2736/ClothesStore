namespace ClothesStore.Api.DTOs.StoreItemStat
{
    public class StoreItemStatDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int VariantId { get; set; }
        public DateOnly StatDate { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ReturnQuantity { get; set; }
    }
}
