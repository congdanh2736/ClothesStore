using ClothesStore.Api.DTOs.Promotion;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _service;

    public PromotionController(IPromotionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromotionDto>> GetById(int id)
    {
        var promotion = await _service.GetByIdAsync(id);
        return promotion is null ? NotFound() : Ok(promotion);
    }

    [HttpPost]
    public async Task<ActionResult<PromotionDto>> Create(CreatePromotionRequest request)
    {
        var (success, error, data) = await _service.CreateAsync(request);
        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePromotionRequest request)
    {
        var (success, error) = await _service.UpdateAsync(id, request);
        if (!success)
            return error == "Không tìm thấy khuyến mãi."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
