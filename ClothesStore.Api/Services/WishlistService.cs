using AutoMapper;
using ClothesStore.Api.DTOs.Wishlist;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _repository;
        private readonly IMapper _mapper;

        public WishlistService(IWishlistRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WishlistDto>> GetAllAsync()
        {
            var wishlists = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<WishlistDto>>(wishlists);
        }

        public async Task<IEnumerable<WishlistDto>> GetByCustomerIdAsync(int customerId)
        {
            var wishlists = await _repository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<WishlistDto>>(wishlists);
        }

        public async Task<(bool Success, string? Error, WishlistDto? Data)> CreateAsync(CreateWishlistDto dto)
        {
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Khách hàng không tồn tại.", null);

            if (!await _repository.ProductExistsAsync(dto.ProductId))
                return (false, "Sản phẩm không tồn tại.", null);

            var existing = await _repository.GetByCustomerAndProductAsync(dto.CustomerId, dto.ProductId);
            if (existing != null)
                return (false, "Sản phẩm đã có trong danh sách yêu thích.", null);

            var wishlist = _mapper.Map<Wishlist>(dto);
            await _repository.AddAsync(wishlist);

            var created = await _repository.GetByIdWithDetailsAsync(wishlist.Id);
            return (true, null, _mapper.Map<WishlistDto>(created));
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var wishlist = await _repository.GetByIdAsync(id);
            if (wishlist is null) return false;
            await _repository.DeleteAsync(wishlist);
            return true;
        }

        public async Task<bool> DeleteByCustomerAndProductAsync(int customerId, int productId)
        {
            var existing = await _repository.GetByCustomerAndProductAsync(customerId, productId);
            if (existing is null) return false;
            await _repository.DeleteAsync(existing);
            return true;
        }

        public async Task<(bool Success, string Message, bool IsWishlisted, WishlistDto? Data)> ToggleAsync(int customerId, int productId)
        {
            if (!await _repository.CustomerExistsAsync(customerId))
                return (false, "Khách hàng không tồn tại.", false, null);

            if (!await _repository.ProductExistsAsync(productId))
                return (false, "Sản phẩm không tồn tại.", false, null);

            var existing = await _repository.GetByCustomerAndProductAsync(customerId, productId);
            if (existing != null)
            {
                await _repository.DeleteAsync(existing);
                return (true, "Đã xóa sản phẩm khỏi danh sách yêu thích.", false, null);
            }

            var wishlist = new Wishlist
            {
                CustomerId = customerId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddAsync(wishlist);

            var created = await _repository.GetByIdWithDetailsAsync(wishlist.Id);
            return (true, "Đã thêm sản phẩm vào danh sách yêu thích.", true, _mapper.Map<WishlistDto>(created));
        }

        public async Task<bool> IsWishlistedAsync(int customerId, int productId)
        {
            return await _repository.IsWishlistedAsync(customerId, productId);
        }
    }
}
