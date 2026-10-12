using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException / NotFoundException
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

        // [THÊM] Tồn kho của 1 cửa hàng
        public async Task<IEnumerable<StoreStockDto>> GetByStoreAsync(int storeId)
        {
            var storeStocks = await _repository.GetByStoreIdAsync(storeId);
            return _mapper.Map<IEnumerable<StoreStockDto>>(storeStocks);
        }

        // [THÊM] Tồn kho của 1 biến thể tại tất cả cửa hàng (Click & Collect)
        public async Task<IEnumerable<StoreStockDto>> GetByVariantAsync(int variantId)
        {
            var storeStocks = await _repository.GetByVariantIdsAsync(new[] { variantId });
            return _mapper.Map<IEnumerable<StoreStockDto>>(storeStocks);
        }

        // [THÊM] Nhập kho (QuantityChange > 0) / xuất kho thủ công (QuantityChange < 0)
        public async Task<StoreStockDto> AdjustAsync(AdjustStockDto dto)
        {
            if (!await _repository.StoreExistsAsync(dto.StoreId))
                throw new NotFoundException("Cửa hàng không tồn tại.");
            if (!await _repository.ProductVariantExistsAsync(dto.VariantId))
                throw new NotFoundException("Biến thể sản phẩm không tồn tại.");

            var storeStock = await _repository.GetByStoreAndVariantAsync(dto.StoreId, dto.VariantId);
            var current = storeStock?.Quantity ?? 0;
            if (current + dto.QuantityChange < 0)
                throw new BusinessException($"Không đủ hàng để xuất (tồn hiện tại: {current}).");

            if (storeStock is null)
            {
                storeStock = new StoreStock
                {
                    StoreId = dto.StoreId,
                    VariantId = dto.VariantId,
                    Quantity = dto.QuantityChange
                };
                await _repository.AddAsync(storeStock);
            }
            else
            {
                storeStock.Quantity += dto.QuantityChange;
                await _repository.UpdateAsync(storeStock);
            }

            var result = await _repository.GetByIdWithDetailsAsync(storeStock.Id);
            return _mapper.Map<StoreStockDto>(result);
        }
    }
}
