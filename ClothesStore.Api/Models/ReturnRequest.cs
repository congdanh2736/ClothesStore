using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    // Bảng 17 - Return_Request: yêu cầu đổi/trả theo từng đơn hàng (ORDER ||--o{ RETURN_REQUEST)
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
    }
}
