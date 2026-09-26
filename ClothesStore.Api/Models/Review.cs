using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Review {
        [Key]
        public int Id { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; } = null;

        public int Rating { get; set; } // 1-5 sao
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}