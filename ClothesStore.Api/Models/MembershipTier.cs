using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class MembershipTier
    {
        [Key]
        public int Id { get; set; }
        public string? TierName { get; set; }

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
