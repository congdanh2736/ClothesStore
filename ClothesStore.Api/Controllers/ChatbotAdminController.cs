// [THÊM MỚI] Chatbot AI - API cho trang quản lý chatbot: /api/ChatbotAdmin/...
using System.Security.Claims;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleGroups.Staff)] // Quản lý + Nhân viên xem được; sửa dữ liệu huấn luyện / train lại chỉ Quản lý
    public class ChatbotAdminController : ControllerBase
    {
        private readonly IChatbotAdminService _service;
        public ChatbotAdminController(IChatbotAdminService service) => _service = service;

        // ------------------------------------------------------------------ Thống kê + lịch sử hội thoại
        /// <summary>Thống kê N ngày gần nhất (mặc định 30)</summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ChatStatsDto>> Stats([FromQuery] int days = 30) => Ok(await _service.GetStatsAsync(days));

        /// <summary>Lịch sử hội thoại có lọc + phân trang. filter = all | low | unhelpful | unreviewed</summary>
        [HttpGet("logs")]
        public async Task<ActionResult<PagedResultDto<ChatLogDto>>> Logs([FromQuery] ChatLogQuery query) => Ok(await _service.GetLogsAsync(query));

        /// <summary>Đánh dấu đã xem, gán intent đúng, đưa vào dữ liệu huấn luyện</summary>
        [HttpPatch("logs/{id:int}/review")]
        public async Task<ActionResult<ChatLogDto>> Review(int id, ReviewChatLogDto dto) => Ok(await _service.ReviewLogAsync(id, dto));

        [Authorize(Roles = RoleGroups.Admin)]
        [HttpDelete("logs/{id:int}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            await _service.DeleteLogAsync(id);
            return NoContent();
        }

        // ------------------------------------------------------------------ Dữ liệu huấn luyện
        /// <summary>Danh sách intent + số câu huấn luyện của từng intent</summary>
        [HttpGet("intents")]
        public async Task<ActionResult<List<ChatIntentInfoDto>>> Intents() => Ok(await _service.GetIntentsAsync());

        /// <summary>Câu huấn luyện do admin thêm (lọc theo intent)</summary>
        [HttpGet("samples")]
        public async Task<ActionResult<List<ChatTrainingSampleDto>>> Samples([FromQuery] string? label) => Ok(await _service.GetSamplesAsync(label));

        [Authorize(Roles = RoleGroups.Admin)]
        [HttpPost("samples")]
        public async Task<ActionResult<ChatTrainingSampleDto>> CreateSample(SaveTrainingSampleDto dto) => Ok(await _service.CreateSampleAsync(dto));

        [Authorize(Roles = RoleGroups.Admin)]
        [HttpPut("samples/{id:int}")]
        public async Task<ActionResult<ChatTrainingSampleDto>> UpdateSample(int id, SaveTrainingSampleDto dto) => Ok(await _service.UpdateSampleAsync(id, dto));

        [Authorize(Roles = RoleGroups.Admin)]
        [HttpDelete("samples/{id:int}")]
        public async Task<IActionResult> DeleteSample(int id)
        {
            await _service.DeleteSampleAsync(id);
            return NoContent();
        }

        // ------------------------------------------------------------------ Mô hình + huấn luyện lại
        /// <summary>Thông tin file mô hình + 10 lần huấn luyện gần nhất</summary>
        [HttpGet("model")]
        public async Task<ActionResult<ChatModelInfoDto>> Model() => Ok(await _service.GetModelInfoAsync());

        /// <summary>Bắt đầu huấn luyện lại (chạy nền vài phút). Trả về lần huấn luyện để hỏi trạng thái.</summary>
        [Authorize(Roles = RoleGroups.Admin)]
        [HttpPost("train")]
        public async Task<ActionResult<ChatTrainingRunDto>> Train()
            => Accepted(await _service.StartTrainingAsync(User.FindFirstValue(ClaimTypes.Email)));

        [HttpGet("train/{id:int}")]
        public async Task<ActionResult<ChatTrainingRunDto>> TrainStatus(int id) => Ok(await _service.GetTrainingAsync(id));
    }
}
