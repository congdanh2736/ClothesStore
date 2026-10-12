// [THÊM] dùng cho [Authorize] (phân quyền)
using Microsoft.AspNetCore.Authorization;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreStockController : ControllerBase
    {
        private readonly IStoreStockService _service;

        public StoreStockController(IStoreStockService service) => _service = service;

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var storeStock = await _service.GetByIdAsync(id);
            return storeStock is null ? NotFound() : Ok(storeStock);
        }

        // [THÊM] Xem tồn theo cửa hàng / theo biến thể, nhập - xuất kho ===== BẮT ĐẦU =====
        // [THÊM] Tồn kho của 1 cửa hàng
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpGet("store/{storeId}")]
        public async Task<IActionResult> GetByStore(int storeId) => Ok(await _service.GetByStoreAsync(storeId));

        // [THÊM] Cửa hàng nào còn biến thể này (Click & Collect) - công khai cho khách
        [HttpGet("variant/{variantId}")]
        public async Task<IActionResult> GetByVariant(int variantId) => Ok(await _service.GetByVariantAsync(variantId));

        // [THÊM] Nhập kho (QuantityChange > 0) / xuất kho (QuantityChange < 0)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto) => Ok(await _service.AdjustAsync(dto));
        // [THÊM] ===== KẾT THÚC =====

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStoreStockDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            return success
                ? CreatedAtAction(nameof(GetById), new { id = data!.Id }, data)
                : BadRequest(new { message = error });
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreStockDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (success) return NoContent();
            return error == "Không tìm thấy tồn kho cửa hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });
        }

        // [THÊM] Phân quyền: chỉ Admin/Employee (trước đây ai cũng gọi được)
        [Authorize(Roles = RoleGroups.Staff)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
