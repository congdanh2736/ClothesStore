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
            // Kiểm tra khách hàng có tồn tại không
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Khách hàng không tồn tại.", null);

            // Kiểm tra sản phẩm có tồn tại không
            if (!await _repository.ProductExistsAsync(dto.ProductId))
                return (false, "Sản phẩm không tồn tại.", null);

            // Kiểm tra xem khách hàng đã thêm sản phẩm này vào wishlist chưa
            var existing = await _repository.GetByCustomerAndProductAsync(dto.CustomerId, dto.ProductId);
            if (existing != null)
                return (false, "Sản phẩm đã có trong danh sách yêu thích.", null);

            var wishlist = _mapper.Map<Wishlist>(dto);
            await _repository.AddAsync(wishlist);

            // Lấy lại entity với navigation properties để map đầy đủ
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
    }
}
