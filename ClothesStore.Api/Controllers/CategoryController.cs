// [XÓA] using không còn dùng: System.Reflection.Metadata.Ecma335; System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization; // [THÊM] dùng cho [Authorize] (phân quyền)
using ClothesStore.Api.Common;           // [THÊM] dùng RoleGroups
using ClothesStore.Api.DTOs.Category;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;
// [XÓA] using không còn dùng: Microsoft.Identity.Client.NativeInterop;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;
        public CategoryController(ICategoryService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<CategoryDTO>>> GetAll()
        {
            var Category = await _service.GetAllAsync();
            return Ok(Category);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDTO>> GetById(int id)
        {
            var Category = await _service.GetByIdAsync(id);
            if( Category == null) 
                return NotFound( new {message = "không tìm thấy danh mục"});
            return Ok(Category);
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (success == false)
                return BadRequest(new {message = error});

            return CreatedAtAction(nameof(GetById),new {id = data!.Id}, data);
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if(success == false )
                return BadRequest(new {message = error});

            return NoContent();
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Detele(int id)
        {
            var Category = await _service.DeleteAsync(id);
            if (Category == false)
                return NotFound(new {message = "Không tìm thấy Danh mục"});
            return NoContent();
        }
    }
}