namespace ClothesStore.Api.DTOs.MembershipTier
{
    public class MembershipTierDto
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        // [THÊM] Mốc điểm tối thiểu để tự động xét hạng
        public int MinPoints { get; set; }
        public int CustomerCount { get; set; }
    }
}
