namespace ClothesStore.Api.DTOs.Employee
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        // [THÊM] Trả riêng họ, tên, cửa hàng và email để trang admin điền sẵn vào form Sửa
        // (thiếu StoreId thì khi Sửa nhân viên sẽ bị mất cửa hàng đang làm)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? StoreId { get; set; }
        public string? Email { get; set; }                // email của tài khoản đăng nhập (ApplicationUser)
    }
}
