using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Review>> GetAllAsync()
            => await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<Review?> GetByIdAsync(int id)
            => await _context.Reviews.FindAsync(id);

        public async Task<Review?> GetByIdWithDetailsAsync(int id)
            => await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Review>> GetByProductIdAsync(int productId)
            => await _context.Reviews
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Review>> GetByCustomerIdAsync(int customerId)
            => await _context.Reviews
                .Include(r => r.Product)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<Review?> GetByCustomerAndProductAsync(int customerId, int productId)
            => await _context.Reviews
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ProductId == productId);

        public async Task<bool> CustomerExistsAsync(int customerId)
            => await _context.Customers.AnyAsync(c => c.Id == customerId);

        public async Task<bool> ProductExistsAsync(int productId)
            => await _context.Products.AnyAsync(p => p.Id == productId);

        public async Task AddAsync(Review entity)
        {
            await _context.Reviews.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Review entity)
        {
            _context.Reviews.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Review entity)
        {
            _context.Reviews.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
