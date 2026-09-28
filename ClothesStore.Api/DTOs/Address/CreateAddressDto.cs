namespace ClothesStore.Api.DTOs.Address
{
    public class CreateAddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public int CustomerId { get; set; }  // Thêm trường CustomerId để liên kết với khách hàng
    }
}
