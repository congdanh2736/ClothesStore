using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class OrderItem {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
         [ForeignKey("OrderId")]
        public int VariantId { get; set; }
        [ForeignKey("VariantId")]
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
    }
}