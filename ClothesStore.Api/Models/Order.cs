using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public int AddressId { get; set; }
        [ForeignKey("AddressId")]
        public int PromotionId { get; set; }
        [ForeignKey("PromotionId")]
        public decimal TotalAmount { get; set;}

        //Navigation
        public ICollection<OrderItem> OrderItems = new List<OrderItem>();
        public PaymentTransaction PaymentTransaction { get; set; } = new PaymentTransaction();

        
    }
}
