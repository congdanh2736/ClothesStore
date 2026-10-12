namespace ClothesStore.Api.DTOs.MembershipTier
{
    public class UpdateMembershipTierDto
    {
        public string TierName { get; set; } = string.Empty;
        // [SỬA] Bỏ CustomerCount (client không được sửa số khách), thêm MinPoints (trước đây: public int CustomerCount { get; set; })
        public int MinPoints { get; set; }
    }
}
