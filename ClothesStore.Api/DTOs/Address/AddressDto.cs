namespace ClothesStore.Api.DTOs.Address
{
    public class AddressDto
    {
        public int Id { get; set; }
        // [THÊM] Trả kèm CustomerId để biết địa chỉ thuộc khách nào
        public int? CustomerId { get; set; }
        public string? Street { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public int OrderCount { get; set; } = 0;  // Số lượng đơn hàng liên quan đến địa chỉ này
    }
}
