using ClothesStore.Api.DTOs.Cart;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/carts")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCart(int customerId)
        {
            var cart = await _cartService.GetCartByCustomerAsync(customerId);
            if (cart == null)
            {
                return NotFound(new { Message = "Giỏ hàng trống." });
            }
            return Ok(new { Success = true, Data = cart });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItemToCart([FromBody] AddToCartRequestDto request)
        {
            try
            {
                var result = await _cartService.AddItemToCartAsync(request);
                return Ok(new { Message = "Thêm sản phẩm vào giỏ hàng thành công.", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("items")]
        public async Task<IActionResult> UpdateItemQuantity([FromBody] UpdateCartItemRequestDto request)
        {
            try
            {
                var result = await _cartService.UpdateCartItemQuantityAsync(request);
                return Ok(new { Message = "Cập nhật số lượng thành công.", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var success = await _cartService.RemoveItemFromCartAsync(cartItemId);
            if (!success)
            {
                return NotFound(new { Message = "Không tìm thấy sản phẩm cần xóa." });
            }
            return Ok(new { Message = "Đã xóa sản phẩm khỏi giỏ hàng." });
        }
    }
}