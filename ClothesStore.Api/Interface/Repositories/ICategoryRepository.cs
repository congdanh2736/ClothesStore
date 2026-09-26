using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<bool> CategoryExistsAsync(int categoryId);
    }
}