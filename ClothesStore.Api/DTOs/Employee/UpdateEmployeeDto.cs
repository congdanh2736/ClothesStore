namespace ClothesStore.Api.DTOs.Employee
{
    public class UpdateEmployeeDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? StoreId { get; set; }
    }
}
