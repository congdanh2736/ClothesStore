namespace ClothesStore.Api.DTOs.Address
{
    public class UpdateAddressDto
    {
        public string? Street { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public int? CustomerId { get; set; }  // Thêm trường CustomerId để liên kết với khách hàng
    }
}
