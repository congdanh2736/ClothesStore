// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: API vận chuyển /api/Shipment (bảng Shipment - trước đây chưa có)
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.Shipment;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipmentController : ControllerBase
    {
        private readonly IShipmentService _service;

        public ShipmentController(IShipmentService service) => _service = service;

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShipmentDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ShipmentDto>> GetById(int id)
        {
            var shipment = await _service.GetByIdAsync(id);
            return shipment is null ? NotFound() : Ok(shipment);
        }

        // Tra cứu vận đơn của 1 đơn hàng
        [HttpGet("order/{orderId:int}")]
        public async Task<ActionResult<ShipmentDto>> GetByOrder(int orderId)
        {
            var shipment = await _service.GetByOrderIdAsync(orderId);
            return shipment is null ? NotFound(new { message = "Đơn hàng chưa có vận đơn." }) : Ok(shipment);
        }

        // Bàn giao đơn đã xác nhận cho đơn vị vận chuyển -> đơn chuyển Shipping
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<ActionResult<ShipmentDto>> Create(CreateShipmentDto dto)
        {
            var shipment = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = shipment.Id }, shipment);
        }

        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ShipmentDto>> Update(int id, UpdateShipmentDto dto)
            => Ok(await _service.UpdateAsync(id, dto));

        // Delivered: hoàn tất đơn (thanh toán COD, cộng điểm, thống kê); Failed: giao thất bại
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<ShipmentDto>> UpdateStatus(int id, UpdateShipmentStatusDto dto)
            => Ok(await _service.UpdateStatusAsync(id, dto));
    }
}
