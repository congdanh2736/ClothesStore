using ClothesStore.Api.DTOs.Wishlist;

namespace ClothesStore.Api.Interface.Services
{
    public interface IWishlistService
    {
        Task<IEnumerable<WishlistDto>> GetAllAsync();
        Task<IEnumerable<WishlistDto>> GetByCustomerIdAsync(int customerId);
        Task<(bool Success, string? Error, WishlistDto? Data)> CreateAsync(CreateWishlistDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
