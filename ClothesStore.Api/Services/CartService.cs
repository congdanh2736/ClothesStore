using ClothesStore.DTOs.Cart;
using ClothesStore.Models;
using ClothesStore.Repositories;

namespace ClothesStore.Services
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

            var cartItem = await _cartRepository.GetCartItemByVariantAsync(cart.CartId, request.VariantId);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
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

            var parentCart = await _cartRepository.GetCartByCustomerIdAsync(
                (await _cartRepository.GetCartItemByIdAsync(request.CartItemId))?.CartId ?? 0
            );

            // Lấy lại theo CustomerId của cart cha
            var fullCart = await _cartRepository.GetCartByCustomerIdAsync(parentCart?.CustomerId ?? 0);
            return fullCart != null ? MapToCartResponseDto(fullCart) : null;
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
                CartId = cart.CartId,
                CustomerId = cart.CustomerId,
                Items = cart.CartItems.Select(ci => new CartItemResponseDto
                {
                    CartItemId = ci.CartItemId,
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity
                }).ToList()
            };
        }
    }
}