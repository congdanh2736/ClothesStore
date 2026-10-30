// [THÊM MỚI] Chatbot AI - API hỏi đáp: POST /api/Chatbot/ask
using System.Security.Claims;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // khách chưa đăng nhập vẫn hỏi được; có gửi token thì bot tra cứu được đơn hàng của chính khách
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _service;

        public ChatbotController(IChatbotService service) => _service = service;

        /// <summary>
        /// Gửi 1 câu hỏi cho chatbot.
        /// Ví dụ body: { "message": "cao 1m70 nặng 65kg mặc áo thun size gì", "context": null }
        /// Câu sau gửi kèm "context" lấy từ phản hồi của câu trước để bot hiểu câu hỏi nối tiếp.
        /// </summary>
        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponseDto>> Ask(ChatRequestDto request)
        {
            // Id tài khoản trong token (JwtService lưu ở ClaimTypes.NameIdentifier); chưa đăng nhập -> null
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _service.AskAsync(request, userId));
        }
    }
}
