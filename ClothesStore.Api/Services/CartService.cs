using ClothesStore.Api.DTOs.Cart;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<CartResponseDto> GetCartByCustomerAsync(int customerId)
        {
            var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId);
            if (cart == null) return null;

            return MapToCartResponseDto(cart);
        }

        public async Task<CartResponseDto> AddItemToCartAsync(AddToCartRequestDto request)
        {
            var cart = await _cartRepository.GetCartByCustomerIdAsync(request.CustomerId);
            if (cart == null)
            {
                cart = new Cart { CustomerId = request.CustomerId };
                await _cartRepository.AddCartAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }

            var cartItem = await _cartRepository.GetCartItemByVariantAsync(cart.Id, request.VariantId);
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
                cartItem.Quantity += request.Quantity;
            }

            await _cartRepository.SaveChangesAsync();
            return await GetCartByCustomerAsync(request.CustomerId);
        }

        public async Task<CartResponseDto> UpdateCartItemQuantityAsync(UpdateCartItemRequestDto request)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(request.CartItemId);
            if (cartItem == null) throw new Exception("Không tìm thấy sản phẩm trong giỏ hàng.");

            if (request.Quantity <= 0)
            {
                _cartRepository.RemoveCartItem(cartItem);
            }
            else
            {
                cartItem.Quantity = request.Quantity;
            }

            await _cartRepository.SaveChangesAsync();

            var cart = await _cartRepository.GetCartByCustomerIdAsync(cartItem.Cart?.CustomerId ?? 0);
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

        private CartResponseDto MapToCartResponseDto(Cart cart)
        {
            return new CartResponseDto
            {
                CartId = cart.Id,
                CustomerId = cart.CustomerId,
                Items = cart.CartItems.Select(ci => new CartItemResponseDto
                {
                    CartItemId = ci.Id,
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity
                }).ToList()
            };
        }
    }
}