using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    // Bảng 23 - Shipment: thông tin giao hàng của đơn (ORDER ||--o| SHIPMENT)
    public class Shipment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        public string Carrier { get; set; } = string.Empty;        // đơn vị vận chuyển (GHN, GHTK, ...)
        public string? TrackingNumber { get; set; }                // mã vận đơn
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
