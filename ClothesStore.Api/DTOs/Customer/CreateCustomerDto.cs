namespace ClothesStore.Api.DTOs.Customer
{
    public class CreateCustomerDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? MembershipTierId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty; 
    }
}
