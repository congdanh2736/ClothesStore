// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [THÊM] Phân quyền: bắt buộc đăng nhập (trước đây không cần token)
    [Authorize]
    public class LoyaltyTransactionController : ControllerBase
    {
        private readonly ILoyaltyTransactionService _service;
        public LoyaltyTransactionController(ILoyaltyTransactionService service)
        {
            _service = service;
        }

        // Phương thức lấy tất cả các giao dịch điểm thưởng
        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoyaltyTransactionDto>>> GetAll()
            // Trả về Ok nghĩa là HTTP 200 kèm dữ liệu danh sách giao dịch điểm thưởng
            => Ok(await _service.GetAllAsync());

        // Phương thức lấy giao dịch điểm thưởng theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<LoyaltyTransactionDto>> GetById(int id)
        {
            var transaction = await _service.GetByIdAsync(id);
            return transaction is null ? NotFound() : Ok(transaction);
        }

        // [THÊM] Lịch sử điểm và tổng điểm/hạng của khách ===== BẮT ĐẦU =====
        // [THÊM] Lịch sử cộng / trừ điểm của 1 khách
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<LoyaltyTransactionDto>>> GetByCustomer(int customerId)
            => Ok(await _service.GetByCustomerIdAsync(customerId));

        // [THÊM] Tổng điểm hiện tại và hạng thành viên của khách
        [HttpGet("customer/{customerId}/points")]
        public async Task<ActionResult<CustomerPointsDto>> GetPoints(int customerId)
        {
            var points = await _service.GetCustomerPointsAsync(customerId);
            return points is null ? NotFound(new { message = "Khách hàng không tồn tại." }) : Ok(points);
        }
        // [THÊM] ===== KẾT THÚC =====

        // Phương thức tạo mới một giao dịch điểm thưởng
        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<ActionResult<LoyaltyTransactionDto>> Create(CreateLoyaltyTransactionDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        // Phương thức cập nhật một giao dịch điểm thưởng
        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateLoyaltyTransactionDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return error == "Không tìm thấy giao dịch điểm thưởng." ? NotFound(new { message = error }) : BadRequest(new { message = error });
            return NoContent();
        }

        // Phương thức xóa một giao dịch điểm thưởng
        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound( new { message = "Không tìm thấy giao dịch điểm thưởng." });
        }
    }
}
