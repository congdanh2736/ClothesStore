using System.ComponentModel.DataAnnotations;

namespace ClothesStore.src.Models
{
    public class Membership_Tier
    {
        [Key]
        public int Id { get; set; }
        public string? TierName { get; set; }
    }
}
