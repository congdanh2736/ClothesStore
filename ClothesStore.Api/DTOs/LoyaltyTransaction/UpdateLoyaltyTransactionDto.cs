namespace ClothesStore.Api.DTOs.LoyaltyTransaction
{
    public class UpdateLoyaltyTransactionDto
    {
        public int CustomerId { get; set; }
        public DateTime TxnDate { get; set; }
        public int PointsChange { get; set; }
        public string? Reason { get; set; }
    }
}
