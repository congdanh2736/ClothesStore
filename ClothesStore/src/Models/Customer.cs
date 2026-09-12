using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.src.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? MembershipTierId { get; set; }
        [ForeignKey("MembershipTierId")]
        public virtual MembershipTier? MembershipTier { get; set; }
    }
}
