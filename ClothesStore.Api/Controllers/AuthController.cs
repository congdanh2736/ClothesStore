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

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResultDto>> Register(RegisterDto dto)
        {
            var (success, error, data) = await _service.RegisterAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return Ok(data);
        }
    }
}
