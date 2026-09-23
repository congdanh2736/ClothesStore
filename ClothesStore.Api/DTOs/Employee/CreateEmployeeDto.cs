namespace ClothesStore.Api.DTOs.Employee
{
    public class CreateEmployeeDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
    }
}
