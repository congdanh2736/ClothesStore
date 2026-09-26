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

        // Thêm sản phẩm vào wishlist
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWishlistDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetAll), new { id = data!.Id }, data);
        }

        // Xóa sản phẩm khỏi wishlist
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
