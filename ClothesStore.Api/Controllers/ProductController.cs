using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase 
    {
        private readonly IProductService _service;
        public ProductController(IProductService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
        {
            var Product = await _service.GetAllAsync();
            return Ok(Product);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var Product = await _service.GetByIdAsync(id);
            if (Product == null)
                return NotFound(new {message = "không tìm thấy sản phẩm"});
            
            return Ok(Product);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if(success == false)
                return BadRequest(new {message = error});

            return CreatedAtAction(nameof(GetById),new {id = data!.Id}, data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var (success,error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return BadRequest(new {message = error});
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var Product = await _service.DeleteAsync(id);
            if (Product == false)
                return NotFound(new {message = "không tìm thấy sản phẩm"});

            return NoContent();
        }
    }
}