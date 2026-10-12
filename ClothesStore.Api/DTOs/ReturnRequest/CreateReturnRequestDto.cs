// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: DTO khách gửi yêu cầu đổi/trả
namespace ClothesStore.Api.DTOs.ReturnRequest
{
    public class CreateReturnRequestDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<CreateReturnItemDto> Items { get; set; } = new();
    }

    public class CreateReturnItemDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string? Condition { get; set; }
    }
}
