// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: API đổi/trả hàng /api/ReturnRequest (bảng Return_Request, Return_Item - trước đây chưa có)
using System.Security.Claims;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.ReturnRequest;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReturnRequestController : ControllerBase
    {
        private readonly IReturnRequestService _service;

        public ReturnRequestController(IReturnRequestService service) => _service = service;

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReturnRequestDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReturnRequestDto>> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id, GetCurrentUserId(), IsStaff());
            return request is null ? NotFound() : Ok(request);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<ActionResult<IEnumerable<ReturnRequestDto>>> GetByOrder(int orderId)
            => Ok(await _service.GetByOrderIdAsync(orderId, GetCurrentUserId(), IsStaff()));

        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<ReturnRequestDto>>> GetByCustomer(int customerId)
            => Ok(await _service.GetByCustomerIdAsync(customerId, GetCurrentUserId(), IsStaff()));

        // Khách gửi yêu cầu đổi/trả cho đơn đã giao thành công
        [HttpPost]
        public async Task<ActionResult<ReturnRequestDto>> Create(CreateReturnRequestDto dto)
        {
            var request = await _service.CreateAsync(dto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPatch("{id:int}/approve")]
        public async Task<ActionResult<ReturnRequestDto>> Approve(int id)
            => Ok(await _service.ApproveAsync(id));

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPatch("{id:int}/reject")]
        public async Task<ActionResult<ReturnRequestDto>> Reject(int id)
            => Ok(await _service.RejectAsync(id));

        // Đã nhận lại hàng: hoàn kho, trừ điểm, cập nhật thanh toán & thống kê
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPatch("{id:int}/complete")]
        public async Task<ActionResult<ReturnRequestDto>> Complete(int id)
            => Ok(await _service.CompleteAsync(id));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id, GetCurrentUserId(), IsStaff()) ? NoContent() : NotFound();

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private bool IsStaff()
            => User.IsInRole(AppRole.Admin.ToString()) || User.IsInRole(AppRole.Employee.ToString());
    }
}
