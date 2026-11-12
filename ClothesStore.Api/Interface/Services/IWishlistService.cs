using ClothesStore.Api.DTOs.Wishlist;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    // Kế thừa IService: GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync đã có sẵn ở interface cha
    public interface IWishlistService : IService<WishlistDto, CreateWishlistDto, UpdateWishlistDto>
    {
        Task<IEnumerable<WishlistDto>> GetByCustomerIdAsync(int customerId);
        Task<bool> DeleteByCustomerAndProductAsync(int customerId, int productId);
        Task<(bool Success, string Message, bool IsWishlisted, WishlistDto? Data)> ToggleAsync(int customerId, int productId);
        Task<bool> IsWishlistedAsync(int customerId, int productId);
    }
}
