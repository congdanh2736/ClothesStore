using ClothesStore.Api.DTOs.Auth;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        /*
         * POST api/auth/register
         * Phương thức này nhận dữ liệu đăng ký từ client, gọi service để thực hiện đăng ký người dùng mới.
         */
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResultDto>> Register(RegisterDto dto)
        {
            /*
             * Gọi service để thực hiện đăng ký người dùng mới
             * (success, error, data) là một tuple chứa kết quả của quá trình đăng ký
             * data ở đây là thông tin của người dùng vừa được tạo, bao gồm email và danh sách vai trò
             */
            var (success, error, data) = await _service.RegisterAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return Ok(data);
        }

        /*
         * POST api/auth/login
         * Phương thức này nhận dữ liệu đăng nhập từ client, gọi service để thực hiện xác thực người dùng.
         */
        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto)
        {
            var (success, error, data) = await _service.LoginAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return Ok(data);
        }
    }
}
