namespace ClothesStore.Api.DTOs.Customer
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? MembershipTierName { get; set; }   // lấy từ navigation, không lộ toàn bộ MembershipTier
        public int AddressCount { get; set; }
        public int WishlistCount { get; set; }
    }
}
