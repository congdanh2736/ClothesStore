using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IMembershipTierRepository : IRepository<MembershipTier>
    {
        Task<bool> MinPointsExistsAsync(int minPoints, int? excludeId = null);
        // Hạng cao nhất mà số điểm đạt được (MinPoints <= points)
        Task<MembershipTier?> GetTierForPointsAsync(int points);
    }
}
