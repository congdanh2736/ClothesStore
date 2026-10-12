// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO trả về của Return_Request và Return_Item
namespace ClothesStore.Api.DTOs.ReturnRequest
{
    public class ReturnRequestDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public DateTime RequestDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalRefund { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public int VariantId { get; set; }
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int Quantity { get; set; }
        public string? Condition { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
