using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class MembershipTier
    {
        [Key]
        public int Id { get; set; }
        public string? TierName { get; set; }

        // Số điểm tích lũy tối thiểu để đạt hạng này (dùng để tự động xét hạng)
        public int MinPoints { get; set; }

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
