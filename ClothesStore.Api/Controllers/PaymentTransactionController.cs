using ClothesStore.Api.Common; // [THÊM] dùng RoleGroups
using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization; // [THÊM] dùng cho [Authorize]
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [THÊM] Phân quyền: bắt buộc đăng nhập
[Authorize]
public class PaymentTransactionController : ControllerBase
{
    private readonly IPaymentTransactionService _service;

    public PaymentTransactionController(IPaymentTransactionService service)
    {
        _service = service;
    }

    // [THÊM] Phân quyền: chỉ Admin/Employee
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentTransactionDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentTransactionDto>> GetById(int id)
    {
        var transaction = await _service.GetByIdAsync(id);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    // [THÊM] Phân quyền: chỉ Admin/Employee
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpPost]
    public async Task<ActionResult<PaymentTransactionDto>> Create(CreatePaymentTransactionRequest request)
    {
        var (success, error, data) = await _service.CreateAsync(request);
        if (!success)
            return error == "Không tìm thấy đơn hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
    }

    // [THÊM comment] Đổi phương thức thanh toán (khi đơn còn Pending)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePaymentTransactionRequest request)
    {
        var (success, error) = await _service.UpdateAsync(id, request);
        if (!success)
            return error == "Không tìm thấy giao dịch thanh toán."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return NoContent();
    }

    // [THÊM] Endpoint ghi nhận kết quả thanh toán (chỉ Admin/Employee): Paid / Failed (callback cổng thanh toán hoặc nhân viên xác nhận)
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdatePaymentStatusRequest request)
    {
        var (success, error) = await _service.UpdateStatusAsync(id, request);
        if (!success)
            return error == "Không tìm thấy giao dịch thanh toán."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return NoContent();
    }

    // [THÊM] Phân quyền: chỉ Admin
    [Authorize(Roles = RoleGroups.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
