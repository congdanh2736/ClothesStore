using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
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

        //Relashionships
        public ICollection<Address> Addresses = new List<Address>();
        public Cart Cart { get; set; } = new Cart();
        public ICollection<Wishlist> Wishlists = new List<Wishlist>();

        // Link to account
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
