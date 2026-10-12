// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
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

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductImageDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success)
                return BadRequest(new { message = error });

            return CreatedAtAction(nameof(GetById), new { id = data!.ImageId }, data);
        }


        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductImageDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return BadRequest(new { message = error });

            return NoContent();
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
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