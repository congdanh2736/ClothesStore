using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class LoyaltyTransaction
    {
        [Key]
        public int Id { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public DateTime TxnDate { get; set; }
        public int PointsChange { get; set; }
        public string? Reason { get; set; }
    }
}
