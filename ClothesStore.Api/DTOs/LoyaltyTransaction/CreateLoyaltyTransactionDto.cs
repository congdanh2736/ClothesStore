namespace ClothesStore.Api.DTOs.LoyaltyTransaction
{
    public class CreateLoyaltyTransactionDto
    {
        public int CustomerId { get; set; }
        public DateTime TxnDate { get; set; }
        public int PointsChange { get; set; }
        public string? Reason { get; set; }
    }
}
