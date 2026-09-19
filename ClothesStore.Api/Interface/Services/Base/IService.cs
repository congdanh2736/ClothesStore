namespace ClothesStore.Api.Interface.Services.Base
{
    public interface IService<T, TCreate, TUpdate> where T : class where TCreate : class where TUpdate : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<(bool Success, string? Error, T? Data)> CreateAsync(TCreate entity);
        Task<(bool Success, string? Error)> UpdateAsync(int id, TUpdate entity);
        Task<bool> DeleteAsync(int id);
    }
}
