using AutoMapper;
using ClothesStore.Api.DTOs.StoreDailyStat;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class StoreDailyStatService : IStoreDailyStatService
    {
        private readonly IStoreDailyStatRepository _repository;
        private readonly IMapper _mapper;

        public StoreDailyStatService(IStoreDailyStatRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StoreDailyStatDto>> GetAllAsync()
        {
            var statistics = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<StoreDailyStatDto>>(statistics);
        }

        public async Task<StoreDailyStatDto?> GetByIdAsync(int id)
        {
            var statistic = await _repository.GetByIdWithDetailsAsync(id);
            return statistic is null ? null : _mapper.Map<StoreDailyStatDto>(statistic);
        }

        public async Task<(bool Success, string? Error, StoreDailyStatDto? Data)> CreateAsync(CreateStoreDailyStatDto dto)
        {
            if (!await _repository.StoreExistsAsync(dto.StoreId)) return (false, "Cửa hàng không tồn tại.", null);
            if (await _repository.GetByStoreAndDateAsync(dto.StoreId, dto.StatDate) is not null)
                return (false, "Thống kê ngày này đã tồn tại cho cửa hàng.", null);

            var statistic = _mapper.Map<StoreDailyStat>(dto);
            await _repository.AddAsync(statistic);
            return (true, null, _mapper.Map<StoreDailyStatDto>(statistic));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateStoreDailyStatDto dto)
        {
            var statistic = await _repository.GetByIdAsync(id);
            if (statistic is null) return (false, "Không tìm thấy thống kê cửa hàng.");

            var duplicate = await _repository.GetByStoreAndDateAsync(statistic.StoreId, dto.StatDate);
            if (duplicate is not null && duplicate.Id != id) return (false, "Thống kê ngày này đã tồn tại cho cửa hàng.");

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
