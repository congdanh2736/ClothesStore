namespace ClothesStore.Api.DTOs.Auth
{
    // Lớp gửi request đăng nhập từ client đến server, chứa thông tin email và password của người dùng.
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
