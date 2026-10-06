using ClothesStore.Api.DTOs.Wishlist;

namespace ClothesStore.Api.Interface.Services
{
    public interface IWishlistService
    {
        Task<IEnumerable<WishlistDto>> GetAllAsync();
        Task<IEnumerable<WishlistDto>> GetByCustomerIdAsync(int customerId);
        Task<(bool Success, string? Error, WishlistDto? Data)> CreateAsync(CreateWishlistDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteByCustomerAndProductAsync(int customerId, int productId);
        Task<(bool Success, string Message, bool IsWishlisted, WishlistDto? Data)> ToggleAsync(int customerId, int productId);
        Task<bool> IsWishlistedAsync(int customerId, int productId);
    }
}
