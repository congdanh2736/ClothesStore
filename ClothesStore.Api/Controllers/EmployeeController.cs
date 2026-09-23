using ClothesStore.Api.DTOs.Employee;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin, Manager")] // Chỉ cho phép Admin truy cập
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;

        public EmployeeController(IEmployeeService service)
        {
            _service = service;
        }

        // Hàm lấy danh sách tất cả các nhân viên
        [HttpGet]
        public async Task<ActionResult<List<EmployeeDto>>> GetAll()
        {
            var employees = await _service.GetAllAsync();
            return Ok(employees);
        }

        // Hàm lấy thông tin chi tiết của một nhân viên theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _service.GetByIdAsync(id);
            if (employee == null)
                return NotFound();
            return Ok(employee);
        }

        // Hàm tạo mới một nhân viên
        public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });

            // Trả về 201 Created kèm header Location trỏ tới GetById(id) của resource vừa tạo,
            // đúng chuẩn REST cho thao tác tạo mới (thay vì chỉ trả 200 OK)
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        // Hàm cập nhật thông tin của một nhân viên theo ID
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateEmployeeDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return error == "Không tìm thấy nhân viên." ? NotFound(new { message = error }) : BadRequest(new { message = error });
            return NoContent();
        }

        // Hàm xóa một nhân viên theo ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
