namespace ClothesStore.Api.DTOs.StoreDailyStat
{
    public class UpdateStoreDailyStatDto
    {
        public DateOnly StatDate { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
