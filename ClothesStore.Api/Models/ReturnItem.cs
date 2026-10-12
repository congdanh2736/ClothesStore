using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    // Bảng 18 - Return_Item: sản phẩm cụ thể trong một yêu cầu đổi/trả
    public class ReturnItem
    {
        [Key]
        public int Id { get; set; }

        public int ReturnRequestId { get; set; }
        [ForeignKey("ReturnRequestId")]
        public ReturnRequest? ReturnRequest { get; set; }

        public int OrderItemId { get; set; }
        [ForeignKey("OrderItemId")]
        public OrderItem? OrderItem { get; set; }

        public int Quantity { get; set; }
        public string? Condition { get; set; }      // tình trạng hàng (nguyên tem, lỗi, ...)
        public decimal RefundAmount { get; set; }
    }
}
