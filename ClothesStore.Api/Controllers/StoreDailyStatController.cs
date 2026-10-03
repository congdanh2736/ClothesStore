using ClothesStore.Api.DTOs.StoreDailyStat;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreDailyStatController : ControllerBase
    {
        private readonly IStoreDailyStatService _service;

        public StoreDailyStatController(IStoreDailyStatService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var statistic = await _service.GetByIdAsync(id);
            return statistic is null ? NotFound() : Ok(statistic);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStoreDailyStatDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            return success
                ? CreatedAtAction(nameof(GetById), new { id = data!.Id }, data)
                : BadRequest(new { message = error });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreDailyStatDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (success) return NoContent();
            return error == "Không tìm thấy thống kê cửa hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
