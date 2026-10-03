using ClothesStore.Api.DTOs.MembershipTier;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class MembershipTierController : ControllerBase
    {
        private readonly IMembershipTierService _service;

        public MembershipTierController(IMembershipTierService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var membershipTiers = await _service.GetAllAsync();
            return Ok(membershipTiers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var membershipTier = await _service.GetByIdAsync(id);
            if (membershipTier == null)
                return NotFound();
            return Ok(membershipTier);
        }

        [HttpPost]  
        public async Task<IActionResult> Create([FromBody] CreateMembershipTierDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMembershipTierDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success) return BadRequest(new { message = error });
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new { message = "Hạng thành viên không tồn tại" });
        }
    }
}
