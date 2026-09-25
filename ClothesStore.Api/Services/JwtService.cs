using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClothesStore.Api.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _config = configuration;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            // Lấy role của người dùng hiện tại
            var roles = await _userManager.GetRolesAsync(user);

            /* Tạo danh sách các claim cho token
             * Thêm các claim cơ bản như Id, Email, và các claim khác nếu cần
             * Claim có nghĩa là một thông tin về người dùng, ví dụ như Id, Email, Role, v.v.
             */
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                //new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            // Gắn từng role vào claim, đây là phần Admin có thể sử dụng để phân quyền
            foreach (var role in roles)
            {
                /*
                 * Role được nhúng vào token
                 * ví dụ là khi dùng [Authorize(Roles = "Admin")] ở controller thì AspNetCore sẽ tự kiểm tra mà mình không cần kiểm tra thủ công
                 */
                claims.Add(new Claim(ClaimTypes.Role, role)); 
            }

            // Tạo khóa bảo mật từ chuỗi bí mật trong cấu hình
            // Khóa này sẽ được sử dụng để ký token, đảm bảo rằng token không bị giả mạo
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            /* 
             * Tạo thông tin xác thực ký token bằng thuật toán HMAC SHA256
             * Thuật toán HMAC SHA256 là một thuật toán mã hóa phổ biến để đảm bảo tính toàn vẹn và xác thực của dữ liệu
             * Thuật toán này sử dụng một khóa bí mật để tạo ra một mã băm (hash) của dữ liệu, giúp xác minh rằng dữ liệu không bị thay đổi
             */
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            /* Tạo token JWT với các thông tin đã chuẩn bị
             * Token sẽ chứa thông tin về issuer (người phát hành), audience (người nhận), các claim, thời gian hết hạn, và thông tin ký
             * Token này giúp xác thực người dùng và phân quyền truy cập trong ứng dụng
             */
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], // Thông tin về người phát hành token, thường là tên hoặc địa chỉ của ứng dụng
                audience: _config["Jwt:Audience"], // Thông tin về người nhận token, thường là tên hoặc địa chỉ của ứng dụng hoặc dịch vụ mà token được gửi đến
                claims: claims, // Danh sách các claim chứa thông tin về người dùng và quyền hạn của họ
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])), // Thời gian hết hạn của token, sau thời gian này token sẽ không còn hợp lệ
                signingCredentials: creds // Thông tin xác thực ký token, giúp đảm bảo rằng token không bị giả mạo và có thể được xác minh bởi người nhận
            );


            // Trả về token dưới dạng chuỗi
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
