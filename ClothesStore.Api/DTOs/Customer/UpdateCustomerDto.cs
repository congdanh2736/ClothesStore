namespace ClothesStore.Api.DTOs.Customer
{
    public class UpdateCustomerDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? MembershipTierId { get; set; }
    }
}
