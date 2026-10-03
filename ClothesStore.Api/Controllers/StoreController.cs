using ClothesStore.Api.DTOs.Store;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _service;

        public StoreController(IStoreService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var store = await _service.GetByIdAsync(id);
            return store is null ? NotFound() : Ok(store);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStoreDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            return success
                ? CreatedAtAction(nameof(GetById), new { id = data!.Id }, data)
                : BadRequest(new { message = error });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (success) return NoContent();
            return error == "Không tìm thấy cửa hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
