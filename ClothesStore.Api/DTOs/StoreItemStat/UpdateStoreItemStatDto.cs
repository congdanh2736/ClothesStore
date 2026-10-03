namespace ClothesStore.Api.DTOs.StoreItemStat
{
    public class UpdateStoreItemStatDto
    {
        public DateOnly StatDate { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ReturnQuantity { get; set; }
    }
}
