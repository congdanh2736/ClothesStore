using Microsoft.AspNetCore.Identity;

namespace ClothesStore.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public Customer? Customer { get; set; }
        public Employee? Employee { get; set; }
    }
}
