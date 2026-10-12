using AutoMapper;
using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    /*
     * [THÊM] Ghi chú nghiệp vụ:
     * Mỗi lần cộng/trừ điểm được ghi 1 dòng LoyaltyTransaction.
     * Tổng điểm của khách = SUM(PointsChange); sau mỗi thay đổi sẽ xét lại hạng thành viên (MembershipTier.MinPoints).
     */
    public class LoyaltyTransactionService : ILoyaltyTransactionService
    {
        private readonly ILoyaltyTransactionRepository _repository;
        private readonly ICustomerRepository _customerRepository;     // [THÊM] để cập nhật hạng của khách
        private readonly IMembershipTierRepository _tierRepository;   // [THÊM] để tìm hạng theo số điểm
        private readonly IMapper _mapper;

        // [SỬA] thêm tham số ICustomerRepository, IMembershipTierRepository
        public LoyaltyTransactionService(
            ILoyaltyTransactionRepository repository,
            ICustomerRepository customerRepository,
            IMembershipTierRepository tierRepository,
            IMapper mapper)
        {
            _repository = repository;
            _customerRepository = customerRepository; // [THÊM]
            _tierRepository = tierRepository;         // [THÊM]
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

        // [THÊM] Lịch sử cộng / trừ điểm của 1 khách
        public async Task<IEnumerable<LoyaltyTransactionDto>> GetByCustomerIdAsync(int customerId)
        {
            var transactions = await _repository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<List<LoyaltyTransactionDto>>(transactions);
        }

        // [THÊM] Tổng điểm hiện tại + hạng thành viên của khách
        public async Task<CustomerPointsDto?> GetCustomerPointsAsync(int customerId)
        {
            var customer = await _customerRepository.GetByIdWithDetailsAsync(customerId);
            if (customer is null) return null;

            return new CustomerPointsDto
            {
                CustomerId = customer.Id,
                TotalPoints = await _repository.GetTotalPointsAsync(customerId),
                MembershipTierId = customer.MembershipTierId,
                MembershipTierName = customer.MembershipTier?.TierName
            };
        }

        // Phương thức tạo giao dịch điểm thưởng mới
        public async Task<(bool Success, string? Error, LoyaltyTransactionDto? Data)> CreateAsync(CreateLoyaltyTransactionDto dto)
        {
            // [THÊM] Khách hàng phải tồn tại (trước đây CustomerId sai -> lỗi khóa ngoại 500)
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Khách hàng không tồn tại.", null);

            // [THÊM] Trừ điểm (đổi voucher...) không được vượt quá số điểm hiện có
            if (dto.PointsChange < 0)
            {
                var current = await _repository.GetTotalPointsAsync(dto.CustomerId);
                if (current + dto.PointsChange < 0)
                    return (false, $"Khách hàng không đủ điểm (hiện có {current} điểm).", null);
            }

            var transaction = _mapper.Map<LoyaltyTransaction>(dto);
            await _repository.AddAsync(transaction);

            // [THÊM] Điểm thay đổi -> xét lại hạng thành viên
            await RecalculateTierAsync(dto.CustomerId);

            return (true, null, _mapper.Map<LoyaltyTransactionDto>(transaction));
        }

        // Phương thức cập nhật giao dịch điểm thưởng
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateLoyaltyTransactionDto dto)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction is null) return (false, "Không tìm thấy giao dịch điểm thưởng.");

            // [THÊM] Khách hàng mới (nếu đổi) phải tồn tại
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Khách hàng không tồn tại.");

            var oldCustomerId = transaction.CustomerId; // [THÊM] nhớ khách cũ để xét lại hạng
            _mapper.Map(dto, transaction);
            await _repository.UpdateAsync(transaction);

            // [THÊM] Xét lại hạng cho khách (cả khách cũ nếu giao dịch được chuyển sang khách khác)
            await RecalculateTierAsync(transaction.CustomerId);
            if (oldCustomerId != transaction.CustomerId)
                await RecalculateTierAsync(oldCustomerId);

            return (true, null);
        }

        // Phương thức xóa giao dịch điểm thưởng
        public async Task<bool> DeleteAsync(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction is null) return false;

            var customerId = transaction.CustomerId; // [THÊM]
            await _repository.DeleteAsync(transaction);
            await RecalculateTierAsync(customerId);  // [THÊM] xóa giao dịch -> tổng điểm đổi -> xét lại hạng
            return true;
        }

        // [THÊM] Cộng (points > 0) / trừ (points < 0) điểm cho khách rồi xét lại hạng.
        //        Được OrderService (hoàn tất đơn) và ReturnRequestService (trả hàng) gọi tự động.
        public async Task AddPointsAsync(int customerId, int points, string reason)
        {
            if (points == 0) return;

            // Khi trừ điểm (trả hàng) mà khách đã tiêu bớt điểm thì chỉ trừ tối đa số điểm còn lại
            if (points < 0)
            {
                var current = await _repository.GetTotalPointsAsync(customerId);
                points = Math.Max(points, -current);
                if (points == 0) return;
            }

            await _repository.AddAsync(new LoyaltyTransaction
            {
                CustomerId = customerId,
                TxnDate = DateTime.UtcNow,
                PointsChange = points,
                Reason = reason
            });

            await RecalculateTierAsync(customerId);
        }

        // [THÊM] Xét hạng: chọn hạng có MinPoints cao nhất mà tổng điểm của khách đạt được
        public async Task RecalculateTierAsync(int customerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer is null) return;

            var totalPoints = await _repository.GetTotalPointsAsync(customerId);
            var tier = await _tierRepository.GetTierForPointsAsync(totalPoints);

            if (customer.MembershipTierId != tier?.Id)
            {
                customer.MembershipTierId = tier?.Id;
                await _customerRepository.UpdateAsync(customer);
            }
        }

        // [THÊM] Xét lại hạng cho toàn bộ khách (dùng khi Admin thêm/sửa mốc điểm của hạng)
        public async Task RecalculateAllTiersAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            foreach (var customer in customers)
                await RecalculateTierAsync(customer.Id);
        }
    }
}
