namespace ClothesStore.Api.DTOs.MembershipTier
{
    public class UpdateMembershipTierDto
    {
        public string TierName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
    }
}
