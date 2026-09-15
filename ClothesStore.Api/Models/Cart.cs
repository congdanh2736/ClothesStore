using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public ICollection<CartItem>? CartItems { get; set; } = new List<CartItem>();
    }
}
