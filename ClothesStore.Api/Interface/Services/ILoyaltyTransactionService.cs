using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface ILoyaltyTransactionService : IService<LoyaltyTransactionDto, CreateLoyaltyTransactionDto, UpdateLoyaltyTransactionDto>
    {
        // [THÊM] Lịch sử điểm + tổng điểm/hạng của khách; cộng trừ điểm tự động; xét hạng ===== BẮT ĐẦU =====
        Task<IEnumerable<LoyaltyTransactionDto>> GetByCustomerIdAsync(int customerId);
        Task<CustomerPointsDto?> GetCustomerPointsAsync(int customerId);

        // [THÊM] Cộng (points > 0) / trừ (points < 0) điểm cho khách rồi xét lại hạng thành viên
        Task AddPointsAsync(int customerId, int points, string reason);

        // [THÊM] Xét lại hạng thành viên dựa trên tổng điểm hiện tại
        Task RecalculateTierAsync(int customerId);
        Task RecalculateAllTiersAsync();
        // [THÊM] ===== KẾT THÚC =====
    }
}
