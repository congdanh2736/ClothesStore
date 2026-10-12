using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AddressId { get; set; }

        // Khuyến mãi là KHÔNG bắt buộc (ERD: PROMOTION ||--o{ ORDER)
        public int? PromotionId { get; set; }

        // Cửa hàng xuất kho cho đơn (dùng để trừ/hoàn Store_Stock và ghi Store_Daily_Stat, Store_Item_Stat)
        public int? StoreId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal DiscountAmount { get; set; }   // số tiền được giảm từ khuyến mãi
        public decimal TotalAmount { get; set; }      // = tổng tiền hàng - DiscountAmount
        public string Status { get; set; } = string.Empty;

        //Navigation
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        [ForeignKey("AddressId")]
        public Address? Address { get; set; }
        [ForeignKey("PromotionId")]
        public Promotion? Promotion { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        // Phải là property (có get; set;) thì EF Core / AutoMapper mới làm việc đúng
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Navigation 1-1: không khởi tạo sẵn để tránh EF INSERT bản ghi rỗng khi Update
        public PaymentTransaction? PaymentTransaction { get; set; }
        public Shipment? Shipment { get; set; }
        public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    }
}
