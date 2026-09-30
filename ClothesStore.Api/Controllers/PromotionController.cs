using AutoMapper;
using ClothesStore.Api.DTOs.Promotion;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionRepository _repository;
    private readonly IMapper _mapper;

    public PromotionController(IPromotionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAll()
        => Ok(_mapper.Map<IEnumerable<PromotionDto>>(await _repository.GetAllAsync()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromotionDto>> GetById(int id)
    {
        var promotion = await _repository.GetByIdAsync(id);
        return promotion is null ? NotFound() : Ok(_mapper.Map<PromotionDto>(promotion));
    }

    [HttpPost]
    public async Task<ActionResult<PromotionDto>> Create(CreatePromotionRequest request)
    {
        if (!IsValidDateRange(request.StartDate, request.EndDate))
            return BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });

        var promotion = _mapper.Map<Promotion>(request);
        await _repository.AddAsync(promotion);
        return CreatedAtAction(nameof(GetById), new { id = promotion.Id }, _mapper.Map<PromotionDto>(promotion));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePromotionRequest request)
    {
        if (!IsValidDateRange(request.StartDate, request.EndDate))
            return BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });

        var promotion = await _repository.GetByIdAsync(id);
        if (promotion is null)
            return NotFound();

        _mapper.Map(request, promotion);
        await _repository.UpdateAsync(promotion);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var promotion = await _repository.GetByIdAsync(id);
        if (promotion is null)
            return NotFound();

        await _repository.DeleteAsync(promotion);
        return NoContent();
    }

    private static bool IsValidDateRange(DateTime startDate, DateTime endDate)
        => endDate > startDate;
}
