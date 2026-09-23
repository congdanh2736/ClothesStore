using AutoMapper;
using ClothesStore.Api.DTOs.StoreItemStat;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class StoreItemStatService : IStoreItemStatService
    {
        private readonly IStoreItemStatRepository _repository;
        private readonly IMapper _mapper;

        public StoreItemStatService(IStoreItemStatRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StoreItemStatDto>> GetAllAsync()
        {
            var statistics = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<StoreItemStatDto>>(statistics);
        }

        public async Task<StoreItemStatDto?> GetByIdAsync(int id)
        {
            var statistic = await _repository.GetByIdWithDetailsAsync(id);
            return statistic is null ? null : _mapper.Map<StoreItemStatDto>(statistic);
        }

        public async Task<(bool Success, string? Error, StoreItemStatDto? Data)> CreateAsync(CreateStoreItemStatDto dto)
        {
            if (!await _repository.StoreExistsAsync(dto.StoreId)) return (false, "Cửa hàng không tồn tại.", null);
            if (!await _repository.ProductVariantExistsAsync(dto.VariantId)) return (false, "Biến thể sản phẩm không tồn tại.", null);
            if (await _repository.GetByStoreVariantAndDateAsync(dto.StoreId, dto.VariantId, dto.StatDate) is not null)
                return (false, "Thống kê sản phẩm ngày này đã tồn tại.", null);

            var statistic = _mapper.Map<StoreItemStat>(dto);
            await _repository.AddAsync(statistic);
            return (true, null, _mapper.Map<StoreItemStatDto>(statistic));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateStoreItemStatDto dto)
        {
            var statistic = await _repository.GetByIdAsync(id);
            if (statistic is null) return (false, "Không tìm thấy thống kê sản phẩm cửa hàng.");

            var duplicate = await _repository.GetByStoreVariantAndDateAsync(statistic.StoreId, statistic.VariantId, dto.StatDate);
            if (duplicate is not null && duplicate.Id != id) return (false, "Thống kê sản phẩm ngày này đã tồn tại.");

            _mapper.Map(dto, statistic);
            await _repository.UpdateAsync(statistic);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var statistic = await _repository.GetByIdAsync(id);
            if (statistic is null) return false;
            await _repository.DeleteAsync(statistic);
            return true;
        }
    }
}
