using ClothesStore.Api.Common; // [THÊM] dùng BusinessException / NotFoundException
using ClothesStore.Api.DTOs.Cart;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IInventoryService _inventory; // [THÊM] để kiểm tra tồn kho khi thêm vào giỏ

        // [SỬA] thêm tham số IInventoryService
        public CartService(ICartRepository cartRepository, IInventoryService inventory)
        {
            _cartRepository = cartRepository;
            _inventory = inventory; // [THÊM]
        }

        public async Task<CartResponseDto?> GetCartByCustomerAsync(int customerId)
        {
            var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId);
            if (cart == null) return null;

            return MapToCartResponseDto(cart);
        }

        public async Task<CartResponseDto> AddItemToCartAsync(AddToCartRequestDto request)
        {
            if (!await _cartRepository.CustomerExistsAsync(request.CustomerId))
                throw new NotFoundException("Khách hàng không tồn tại.");

            if (!await _cartRepository.VariantExistsAsync(request.VariantId))
                throw new NotFoundException("Biến thể sản phẩm không tồn tại.");

            var cart = await _cartRepository.GetCartByCustomerIdAsync(request.CustomerId);
            if (cart == null)
            {
                cart = new Cart { CustomerId = request.CustomerId };
                await _cartRepository.AddCartAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }

            var cartItem = await _cartRepository.GetCartItemByVariantAsync(cart.Id, request.VariantId);
            // [THÊM] Tính số lượng sau khi cộng dồn
            var newQuantity = (cartItem?.Quantity ?? 0) + request.Quantity;

            // [THÊM] Không cho thêm quá tổng tồn kho của tất cả cửa hàng
            await EnsureStockAsync(request.VariantId, newQuantity);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity
                };
                await _cartRepository.AddCartItemAsync(cartItem);
            }
            else
            {
                cartItem.Quantity = newQuantity; // [SỬA] dùng số lượng đã tính sẵn ở trên (cùng giá trị += cũ)
            }

            await _cartRepository.SaveChangesAsync();
            var updatedCart = await GetCartByCustomerAsync(request.CustomerId);
            return updatedCart!;
        }

        public async Task<CartResponseDto> UpdateCartItemQuantityAsync(UpdateCartItemRequestDto request)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(request.CartItemId);
            // [SỬA] Exception -> NotFoundException
            if (cartItem == null) throw new NotFoundException("Không tìm thấy sản phẩm trong giỏ hàng.");

            var customerId = cartItem.Cart?.CustomerId ?? 0;

            if (request.Quantity <= 0)
            {
                _cartRepository.RemoveCartItem(cartItem);
            }
            else
            {
                await EnsureStockAsync(cartItem.VariantId, request.Quantity); // [THÊM] kiểm tra tồn kho
                cartItem.Quantity = request.Quantity;
            }

            await _cartRepository.SaveChangesAsync();

            var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId);
            if (cart == null) throw new NotFoundException("Không tìm thấy giỏ hàng sau khi cập nhật.");

            return MapToCartResponseDto(cart);
        }

        public async Task<bool> RemoveItemFromCartAsync(int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId);
            if (cartItem == null) return false;

            _cartRepository.RemoveCartItem(cartItem);
            await _cartRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearCartAsync(int customerId)
        {
            var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId);
            if (cart == null) return false;

            await _cartRepository.ClearCartAsync(cart.Id);
            return true;
        }

        private async Task EnsureStockAsync(int variantId, int quantity)
        {
            var totalStock = await _inventory.GetTotalStockAsync(variantId);
            if (quantity > totalStock)
                throw new BusinessException($"Sản phẩm chỉ còn {totalStock} chiếc trong kho.");
        }

        private CartResponseDto MapToCartResponseDto(Cart cart)
        {
            return new CartResponseDto
            {
                CartId = cart.Id,
                CustomerId = cart.CustomerId,
                Items = cart.CartItems?.Select(ci => new CartItemResponseDto
                {
                    CartItemId = ci.Id,
                    VariantId = ci.VariantId,
                    ProductId = ci.ProductVariant?.ProductId ?? 0,
                    ProductName = ci.ProductVariant?.Product?.Name,
                    ProductImageUrl = ci.ProductVariant?.Product?.ProductImages?.FirstOrDefault()?.ImageUrl,
                    Color = ci.ProductVariant?.Color,
                    Size = ci.ProductVariant?.Size,
                    Price = ci.ProductVariant?.Price ?? 0,
                    Quantity = ci.Quantity
                }).ToList() ?? new List<CartItemResponseDto>()
            };
        }
    }
}
