using ClothesStore.Api.DTOs.Wishlist;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _service;

        public WishlistController(IWishlistService service) => _service = service;

        // Lấy tất cả wishlist
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        // Lấy danh sách wishlist theo CustomerId
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
            => Ok(await _service.GetByCustomerIdAsync(customerId));

        // Kiểm tra xem sản phẩm đã có trong wishlist chưa
        [HttpGet("check")]
        public async Task<IActionResult> CheckWishlist([FromQuery] int customerId, [FromQuery] int productId)
        {
            var isWishlisted = await _service.IsWishlistedAsync(customerId, productId);
            return Ok(new { IsWishlisted = isWishlisted });
        }

        // Bật/tắt trạng thái yêu thích (Toggle)
        [HttpPost("toggle")]
        public async Task<IActionResult> Toggle([FromQuery] int customerId, [FromQuery] int productId)
        {
            var (success, message, isWishlisted, data) = await _service.ToggleAsync(customerId, productId);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, isWishlisted, data });
        }

        // Thêm sản phẩm vào wishlist
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWishlistDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetAll), new { id = data!.Id }, data);
        }

        // Xóa sản phẩm khỏi wishlist theo Wishlist Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();

        // Xóa sản phẩm khỏi wishlist theo CustomerId và ProductId
        [HttpDelete("customer/{customerId}/product/{productId}")]
        public async Task<IActionResult> DeleteByCustomerAndProduct(int customerId, int productId)
            => await _service.DeleteByCustomerAndProductAsync(customerId, productId) ? NoContent() : NotFound();
    }
}
