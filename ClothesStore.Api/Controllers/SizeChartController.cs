// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.SizeChart;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SizeChartController : ControllerBase
    {
        private readonly ISizeChartService _service;
        public SizeChartController(ISizeChartService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<SizeChartDto>>> GetAll()
        {
            var SizeChart = await _service.GetAllAsync();
            return Ok(SizeChart);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<SizeChartDto>> GetById(int id)
        {
            var SizeChart = await _service.GetByIdAsync(id);
            if (SizeChart == null)
                return NotFound(new { message = "không tìm thấy bản size" });

            return Ok(SizeChart);
        }

        // [THÊM] Tra cứu bảng size theo danh mục ===== BẮT ĐẦU =====
        // [THÊM] Tra cứu bảng size theo danh mục
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<SizeChartDto>>> GetByCategory(int categoryId)
            => Ok(await _service.GetByCategoryAsync(categoryId));
        // [THÊM] ===== KẾT THÚC =====

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSizeChartDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success)
                return BadRequest(new { message = error });

            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }


        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSizeChartDto dto)
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
            var SizeChart = await _service.DeleteAsync(id);
            if (!SizeChart)
                return NotFound(new { message = "không tìm thấy bản size" });

            return NoContent();
        }

    }

}