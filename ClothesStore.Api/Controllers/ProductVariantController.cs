// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.ProductVariant;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductVariantController : ControllerBase
    {
        private readonly IProductVariantService _service;
        public ProductVariantController(IProductVariantService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<ProductVariantDto>>> GetAll()
        {
            var ProductVariant = await _service.GetAllAsync();
            return Ok(ProductVariant);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ProductVariantDto>> GetById(int id)
        {
            var ProductVariant = await _service.GetByIdAsync(id);
            if (ProductVariant == null)
                return NotFound(new {message = "không tìm thấy phiên bản sản phẩm"});

            return Ok(ProductVariant);
        }

        // [THÊM] Danh sách biến thể của 1 sản phẩm ===== BẮT ĐẦU =====
        // [THÊM] Danh sách biến thể (màu/size/giá) của 1 sản phẩm
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<List<ProductVariantDto>>> GetByProduct(int productId)
            => Ok(await _service.GetByProductAsync(productId));
        // [THÊM] ===== KẾT THÚC =====

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductVariantDto dto)
        {
            var (success,error,data) = await _service.CreateAsync(dto);
            if (!success)
                return BadRequest(new { message = error});

            return CreatedAtAction(nameof(GetById), new {id = data!.Id}, data);
        }


        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductVariantDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id,dto);
            if (!success)
                return BadRequest(new { message = error });

            return NoContent();
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ProductVariant = await _service.DeleteAsync(id);
            if (!ProductVariant)
                return NotFound(new { message = "không tìm thấy phiên bản sản phẩm" });

            return NoContent();
        }
        
    }
}