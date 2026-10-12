// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO tổng điểm + hạng thành viên của khách
namespace ClothesStore.Api.DTOs.LoyaltyTransaction
{
    public class CustomerPointsDto
    {
        public int CustomerId { get; set; }
        public int TotalPoints { get; set; }
        public int? MembershipTierId { get; set; }
        public string? MembershipTierName { get; set; }
    }
}
