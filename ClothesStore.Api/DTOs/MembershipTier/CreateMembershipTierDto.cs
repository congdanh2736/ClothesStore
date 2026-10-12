namespace ClothesStore.Api.DTOs.MembershipTier
{
    public class CreateMembershipTierDto
    {
        public string TierName { get; set; } = string.Empty;
        // [THÊM] Mốc điểm tối thiểu để tự động xét hạng
        public int MinPoints { get; set; }
    }
}
