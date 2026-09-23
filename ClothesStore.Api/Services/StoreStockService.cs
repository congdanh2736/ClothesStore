using AutoMapper;
using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class StoreStockService : IStoreStockService
    {
        private readonly IStoreStockRepository _repository;
        private readonly IMapper _mapper;

        public StoreStockService(IStoreStockRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StoreStockDto>> GetAllAsync()
        {
            var storeStocks = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<StoreStockDto>>(storeStocks);
        }

        public async Task<StoreStockDto?> GetByIdAsync(int id)
        {
            var storeStock = await _repository.GetByIdWithDetailsAsync(id);
            return storeStock is null ? null : _mapper.Map<StoreStockDto>(storeStock);
        }

        public async Task<(bool Success, string? Error, StoreStockDto? Data)> CreateAsync(CreateStoreStockDto dto)
        {
            if (!await _repository.StoreExistsAsync(dto.StoreId)) return (false, "Cửa hàng không tồn tại.", null);
            if (!await _repository.ProductVariantExistsAsync(dto.VariantId)) return (false, "Biến thể sản phẩm không tồn tại.", null);
            if (await _repository.GetByStoreAndVariantAsync(dto.StoreId, dto.VariantId) is not null)
                return (false, "Tồn kho cho cửa hàng và biến thể này đã tồn tại.", null);

            var storeStock = _mapper.Map<StoreStock>(dto);
            await _repository.AddAsync(storeStock);
            return (true, null, _mapper.Map<StoreStockDto>(storeStock));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateStoreStockDto dto)
        {
            var storeStock = await _repository.GetByIdAsync(id);
            if (storeStock is null) return (false, "Không tìm thấy tồn kho cửa hàng.");

            _mapper.Map(dto, storeStock);
            await _repository.UpdateAsync(storeStock);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var storeStock = await _repository.GetByIdAsync(id);
            if (storeStock is null) return false;

            await _repository.DeleteAsync(storeStock);
            return true;
        }
    }
}
