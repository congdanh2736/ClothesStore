using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IProductVariantRepository : IRepository<ProductVariant>
    {
        Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId);
        Task<bool> ProductExistsAsync(int productId);
        Task<bool> DuplicateExistsAsync(int productId, string? color, string? size, int? excludeVariantId = null);
        // Biến thể đã có trong đơn hàng thì không được xóa
        Task<bool> HasOrdersAsync(int variantId);
    }
}
