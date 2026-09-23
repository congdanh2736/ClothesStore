using AutoMapper;
using ClothesStore.Api.DTOs.Store;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class StoreService : IStoreService
    {
        private readonly IStoreRepository _repository;
        private readonly IMapper _mapper;

        public StoreService(IStoreRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StoreDto>> GetAllAsync()
        {
            var stores = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<StoreDto>>(stores);
        }

        public async Task<StoreDto?> GetByIdAsync(int id)
        {
            var store = await _repository.GetByIdWithDetailsAsync(id);
            return store is null ? null : _mapper.Map<StoreDto>(store);
        }

        public async Task<(bool Success, string? Error, StoreDto? Data)> CreateAsync(CreateStoreDto dto)
        {
            var store = _mapper.Map<Store>(dto);
            await _repository.AddAsync(store);
            return (true, null, _mapper.Map<StoreDto>(store));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateStoreDto dto)
        {
            var store = await _repository.GetByIdAsync(id);
            if (store is null) return (false, "Không tìm thấy cửa hàng.");

            _mapper.Map(dto, store);
            await _repository.UpdateAsync(store);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var store = await _repository.GetByIdAsync(id);
            if (store is null) return false;

            await _repository.DeleteAsync(store);
            return true;
        }
    }
}
