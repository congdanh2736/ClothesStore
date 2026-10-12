namespace ClothesStore.Api.DTOs.Auth
{
    // Lớp trả về kết quả đăng nhập từ server đến client, chứa thông tin token, email và danh sách vai trò của người dùng.
    public class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();

        // [THÊM] Id hồ sơ tương ứng để client gọi các API giỏ hàng / đơn hàng (null nếu tài khoản không phải loại đó)
        public int? CustomerId { get; set; }
        public int? EmployeeId { get; set; }
        public string? FullName { get; set; }
    }
}
