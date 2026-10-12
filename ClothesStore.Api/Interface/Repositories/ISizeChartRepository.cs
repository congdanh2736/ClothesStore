using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ISizeChartRepository : IRepository<SizeChart>
    {
        Task<IEnumerable<SizeChart>> GetByCategoryIdAsync(int categoryId);
        Task<bool> CategoryExistsAsync(int categoryId);
    }
}
