using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }

        //Navigation
        [ForeignKey("OrderId")]
        public Order Order { get; set ;}
    }
}