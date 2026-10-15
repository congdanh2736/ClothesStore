namespace ClothesStore.Api.DTOs.Customer
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        // [THÊM] Trả riêng họ, tên và email để trang admin điền sẵn vào form Sửa
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }                // email của tài khoản đăng nhập (ApplicationUser)
        // [THÊM] Trả kèm Id hạng
        public int? MembershipTierId { get; set; }
        public string? MembershipTierName { get; set; }   // lấy từ navigation, không lộ toàn bộ MembershipTier
        // [THÊM] Tổng điểm tích lũy (SUM Loyalty_Transaction)
        public int TotalPoints { get; set; }              // tổng điểm tích lũy (từ Loyalty_Transaction)
        public int AddressCount { get; set; }
        public int WishlistCount { get; set; }
    }
}
