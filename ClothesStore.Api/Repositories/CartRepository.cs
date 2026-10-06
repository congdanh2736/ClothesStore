using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByCustomerIdAsync(int customerId)
        {
            return await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                            .ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<CartItem?> GetCartItemByIdAsync(int cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }

        public async Task<CartItem?> GetCartItemByVariantAsync(int cartId, int variantId)
        {
            return await _context.CartItems
                .Include(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.VariantId == variantId);
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public async Task AddCartItemAsync(CartItem cartItem)
        {
            await _context.CartItems.AddAsync(cartItem);
        }

        public void RemoveCartItem(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
        }

        public async Task<bool> VariantExistsAsync(int variantId)
        {
            return await _context.ProductVariants.AnyAsync(pv => pv.VariantId == variantId);
        }

        public async Task ClearCartAsync(int cartId)
        {
            var items = await _context.CartItems.Where(ci => ci.CartId == cartId).ToListAsync();
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}