using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int CartId { get; set; }

        public int VariantId { get; set; }

        public int Quantity { get; set; }

        // Navigation properties
        [ForeignKey("CartId")]
        public Cart? Cart { get; set; }

        [ForeignKey("VariantId")]
        public ProductVariant? ProductVariant { get; set; }
    }
}