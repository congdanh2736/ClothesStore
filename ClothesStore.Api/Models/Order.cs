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
        public int PromotionId { get; set; }
        public decimal TotalAmount { get; set;}

        //Navigation
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        [ForeignKey("AddressId")]
        public Address? Address { get; set; }
        [ForeignKey("PromotionId")]
        public Promotion? Promotion { get; set; }
        public ICollection<OrderItem> OrderItems = new List<OrderItem>();
        public PaymentTransaction PaymentTransaction { get; set; } = new PaymentTransaction();

        
    }
}
