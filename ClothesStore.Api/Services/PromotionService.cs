using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Promotion;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PromotionService(IPromotionRepository repository, ApplicationDbContext context, IMapper mapper)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PromotionDto>> GetAllAsync()
        => _mapper.Map<IEnumerable<PromotionDto>>(await _repository.GetAllAsync());

    public async Task<PromotionDto?> GetByIdAsync(int id)
    {
        var promotion = await _repository.GetByIdAsync(id);
        return promotion is null ? null : _mapper.Map<PromotionDto>(promotion);
    }

    public async Task<(bool Success, string? Error, PromotionDto? Data)> CreateAsync(CreatePromotionRequest request)
    {
        if (!HasValidDateRange(request.StartDate, request.EndDate))
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.", null);

        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Mã khuyến mãi là bắt buộc.", null);

        if (await _context.Promotions.AnyAsync(promotion => promotion.Code == code))
            return (false, "Mã khuyến mãi đã tồn tại.", null);

        var promotion = _mapper.Map<Promotion>(request);
        promotion.Code = code;
        await _repository.AddAsync(promotion);
        return (true, null, _mapper.Map<PromotionDto>(promotion));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdatePromotionRequest request)
    {
        if (!HasValidDateRange(request.StartDate, request.EndDate))
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.");

        var promotion = await _repository.GetByIdAsync(id);
        if (promotion is null)
            return (false, "Không tìm thấy khuyến mãi.");

        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Mã khuyến mãi là bắt buộc.");

        if (await _context.Promotions.AnyAsync(item => item.Id != id && item.Code == code))
            return (false, "Mã khuyến mãi đã tồn tại.");

        _mapper.Map(request, promotion);
        promotion.Code = code;
        await _repository.UpdateAsync(promotion);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var promotion = await _repository.GetByIdWithDetailsAsync(id);
        if (promotion is null || promotion.Orders.Any())
            return false;

        await _repository.DeleteAsync(promotion);
        return true;
    }

    private static bool HasValidDateRange(DateTime startDate, DateTime endDate)
        => endDate > startDate;
}
