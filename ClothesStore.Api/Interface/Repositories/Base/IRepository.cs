using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ClothesStore.Api.Interface.Repositories.Base
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdWithDetailsAsync(int id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
