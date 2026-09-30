using AutoMapper;
using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class LoyaltyTransactionService : ILoyaltyTransactionService
    {
        private readonly ILoyaltyTransactionRepository _repository;
        private readonly IMapper _mapper;

        public LoyaltyTransactionService(ILoyaltyTransactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Phương thức lấy tất cả các giao dịch điểm thưởng
        public async Task<IEnumerable<LoyaltyTransactionDto>> GetAllAsync()
        {
            var transactions = await _repository.GetAllAsync();
            return _mapper.Map<List<LoyaltyTransactionDto>>(transactions);
        }

        // Phương thức lấy giao dịch điểm thưởng theo ID
        public async Task<LoyaltyTransactionDto?> GetByIdAsync(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            return transaction is null ? null : _mapper.Map<LoyaltyTransactionDto>(transaction);
        }

        // Phương thức tạo giao dịch điểm thưởng mới
        public async Task<(bool Success, string? Error, LoyaltyTransactionDto? Data)> CreateAsync(CreateLoyaltyTransactionDto dto)
        {
            var transaction = _mapper.Map<LoyaltyTransaction>(dto);
            await _repository.AddAsync(transaction);
            return (true, null, _mapper.Map<LoyaltyTransactionDto>(transaction));
        }

        // Phương thức cập nhật giao dịch điểm thưởng
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateLoyaltyTransactionDto dto)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction is null) return (false, "Không tìm thấy giao dịch điểm thưởng.");
            _mapper.Map(dto, transaction);
            await _repository.UpdateAsync(transaction);
            return (true, null);
        }

        // Phương thức xóa giao dịch điểm thưởng
        public async Task<bool> DeleteAsync(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction is null) return false;
            await _repository.DeleteAsync(transaction);
            return true;
        }
    }
}
