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

        public int? MembershipTierId { get; set; }
        [ForeignKey("MembershipTierId")]
        public virtual MembershipTier? MembershipTier { get; set; }

        //Relashionships
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        // Không khởi tạo sẵn navigation 1-1 (new Cart()) vì EF sẽ hiểu là Cart mới và INSERT trùng khi Update Customer
        public Cart? Cart { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

        // Link to account
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
