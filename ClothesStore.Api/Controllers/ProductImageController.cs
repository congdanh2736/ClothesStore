using ClothesStore.Api.DTOs.ProductImage;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductImageController : ControllerBase
    {
        private readonly IProductImageService _service;
        public ProductImageController(IProductImageService service) => _service = service;


        [HttpGet]
        public async Task<ActionResult<List<ProductImageDto>>> GetAll()
        {
            var ProductImage = await _service.GetAllAsync();
            return Ok(ProductImage);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ProductImageDto>> GetById(int id)
        {
            var ProductImage = await _service.GetByIdAsync(id);
            if (ProductImage == null)
                return NotFound(new { message = "không tìm thấy ảnh của sản phẩm" });

            return Ok(ProductImage);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductImageDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success)
                return BadRequest(new { message = error });

            return CreatedAtAction(nameof(GetById), new { id = data!.ImageId }, data);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductImageDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return BadRequest(new { message = error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ProductImage = await _service.DeleteAsync(id);
            if (!ProductImage)
                return NotFound(new { message = "không tìm thấy ảnh của sản phẩm" });

            return NoContent();
        }

    }
}