using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly ApplicationDbContext _context;

        public WishlistRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Wishlist>> GetAllAsync()
            => await _context.Wishlists
                .Include(w => w.Customer)
                .Include(w => w.Product)
                .ToListAsync();

        public async Task<Wishlist?> GetByIdAsync(int id)
            => await _context.Wishlists.FindAsync(id);

        public async Task<Wishlist?> GetByIdWithDetailsAsync(int id)
            => await _context.Wishlists
                .Include(w => w.Customer)
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.Id == id);

        public async Task<IEnumerable<Wishlist>> GetByCustomerIdAsync(int customerId)
            => await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

        public async Task<Wishlist?> GetByCustomerAndProductAsync(int customerId, int productId)
            => await _context.Wishlists
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.ProductId == productId);

        public async Task<bool> CustomerExistsAsync(int customerId)
            => await _context.Customers.AnyAsync(c => c.Id == customerId);

        public async Task<bool> ProductExistsAsync(int productId)
            => await _context.Products.AnyAsync(p => p.Id == productId);

        public async Task AddAsync(Wishlist entity)
        {
            await _context.Wishlists.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Wishlist entity)
        {
            _context.Wishlists.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Wishlist entity)
        {
            _context.Wishlists.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
