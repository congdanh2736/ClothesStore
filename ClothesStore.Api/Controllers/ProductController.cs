// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
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

        // [THÊM] Chi tiết sản phẩm, lọc theo danh mục, gắn/gỡ bộ sưu tập ===== BẮT ĐẦU =====
        // [THÊM] Chi tiết sản phẩm: ảnh, biến thể (màu/size/giá), bộ sưu tập - công nghệ
        [HttpGet("{id}/detail")]
        public async Task<ActionResult<ProductDetailDto>> GetDetail(int id)
        {
            var product = await _service.GetDetailAsync(id);
            if (product == null)
                return NotFound(new {message = "không tìm thấy sản phẩm"});
            return Ok(product);
        }

        // [THÊM] Lọc sản phẩm theo danh mục
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<ProductDto>>> GetByCategory(int categoryId)
            => Ok(await _service.GetByCategoryAsync(categoryId));

        // [THÊM] Gắn sản phẩm vào bộ sưu tập / công nghệ (bảng Product_Collection)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost("{id}/collections/{collectionId}")]
        public async Task<IActionResult> AddToCollection(int id, int collectionId)
        {
            var (success, error) = await _service.AddToCollectionAsync(id, collectionId);
            if (!success)
                return BadRequest(new { message = error });
            return NoContent();
        }

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpDelete("{id}/collections/{collectionId}")]
        public async Task<IActionResult> RemoveFromCollection(int id, int collectionId)
        {
            var (success, error) = await _service.RemoveFromCollectionAsync(id, collectionId);
            if (!success)
                return NotFound(new { message = error });
            return NoContent();
        }
        // [THÊM] ===== KẾT THÚC =====

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if(success == false)
                return BadRequest(new {message = error});

            return CreatedAtAction(nameof(GetById),new {id = data!.Id}, data);
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var (success,error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return BadRequest(new {message = error});
            
            return NoContent();
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
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