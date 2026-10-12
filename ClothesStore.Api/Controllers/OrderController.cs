using ClothesStore.Api.Common; // [THÊM] dùng RoleGroups
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization; // [THÊM] dùng cho [Authorize]
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [THÊM] Phân quyền: bắt buộc đăng nhập cho mọi API đơn hàng
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;

    public OrderController(IOrderService service)
    {
        _service = service;
    }

    // [THÊM] Phân quyền: chỉ Admin/Employee xem toàn bộ đơn
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDetailDTO>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDTO>> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    // [THÊM] Lịch sử đơn hàng của 1 khách
    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<IEnumerable<OrderDetailDTO>>> GetByCustomer(int customerId)
        => Ok(await _service.GetByCustomerIdAsync(customerId));

    // [THÊM] Đặt hàng từ giỏ hàng (luồng chính): tạo đơn, trừ kho, tạo thanh toán, làm trống giỏ
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDetailDTO>> Checkout(CheckoutRequest request)
    {
        var order = await _service.CheckoutAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // [THÊM comment] Tạo đơn trực tiếp với danh sách sản phẩm (nhân viên tạo đơn tại quầy / test)
    [HttpPost]
    public async Task<ActionResult<OrderDetailDTO>> Create(CreateOrderRequest request)
    {
        var (success, error, data) = await _service.CreateAsync(request);
        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
    }

    // [THÊM comment] Chỉ sửa được khi đơn còn Pending (kiểm tra trong OrderService)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateOrderRequest request)
    {
        var (success, error) = await _service.UpdateAsync(id, request);
        if (!success)
            return error == "Không tìm thấy đơn hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return NoContent();
    }

    // [THÊM] Pending -> Confirmed (chỉ Admin/Employee)
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpPatch("{id:int}/confirm")]
    public async Task<ActionResult<OrderDetailDTO>> Confirm(int id)
        => Ok(await _service.ConfirmAsync(id));

    // [THÊM] Hủy đơn: hoàn kho, hủy / hoàn tiền thanh toán
    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<OrderDetailDTO>> Cancel(int id)
        => Ok(await _service.CancelAsync(id));

    // [THÊM] Khách nhận hàng tại cửa hàng (Click & Collect) -> Completed (chỉ Admin/Employee)
    // (đơn giao qua vận chuyển thì hoàn tất bằng PATCH /api/Shipment/{id}/status = Delivered)
    [Authorize(Roles = RoleGroups.Staff)]
    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<OrderDetailDTO>> Complete(int id)
        => Ok(await _service.CompleteAsync(id));

    // [THÊM] Phân quyền: chỉ Admin; [SỬA] service chỉ cho xóa đơn đã hủy
    [Authorize(Roles = RoleGroups.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
