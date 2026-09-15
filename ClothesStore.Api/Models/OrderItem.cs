using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class OrderItem {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        //Navigation
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
        [ForeignKey("VariantId")]
        public ProductVariant ProductVariant { get; set;}
        
    }
}