namespace ClothesStore.Api.DTOs.MembershipTier
{
    public class MembershipTierDto
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
    }
}
