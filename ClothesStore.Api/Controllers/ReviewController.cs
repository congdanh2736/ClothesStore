using ClothesStore.Api.DTOs.Review;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewController(IReviewService service) => _service = service;

        // Lấy tất cả đánh giá
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        // Lấy đánh giá theo Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var review = await _service.GetByIdAsync(id);
            return review is null ? NotFound() : Ok(review);
        }

        // Lấy danh sách đánh giá theo ProductId
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
            => Ok(await _service.GetByProductIdAsync(productId));

        // Lấy danh sách đánh giá theo CustomerId
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
            => Ok(await _service.GetByCustomerIdAsync(customerId));

        // Tạo đánh giá mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        // Cập nhật đánh giá
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (success) return NoContent();
            return error == "Không tìm thấy đánh giá."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });
        }

        // Xóa đánh giá
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
