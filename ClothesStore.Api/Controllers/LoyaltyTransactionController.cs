using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyTransactionController : ControllerBase
    {
        private readonly ILoyaltyTransactionService _service;
        public LoyaltyTransactionController(ILoyaltyTransactionService service)
        {
            _service = service;
        }

        // Phương thức lấy tất cả các giao dịch điểm thưởng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoyaltyTransactionDto>>> GetAll()
            // Trả về Ok nghĩa là HTTP 200 kèm dữ liệu danh sách giao dịch điểm thưởng
            => Ok(await _service.GetAllAsync());

        // Phương thức lấy giao dịch điểm thưởng theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<LoyaltyTransactionDto>> GetById(int id)
        {
            var transaction = await _service.GetByIdAsync(id);
            return transaction is null ? NotFound() : Ok(transaction);
        }

        // Phương thức tạo mới một giao dịch điểm thưởng
        [HttpPost]
        public async Task<ActionResult<LoyaltyTransactionDto>> Create(CreateLoyaltyTransactionDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        // Phương thức cập nhật một giao dịch điểm thưởng
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateLoyaltyTransactionDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return error == "Không tìm thấy giao dịch điểm thưởng." ? NotFound(new { message = error }) : BadRequest(new { message = error });
            return NoContent();
        }

        // Phương thức xóa một giao dịch điểm thưởng
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound( new { message = "Không tìm thấy giao dịch điểm thưởng." });
        }
    }
}
