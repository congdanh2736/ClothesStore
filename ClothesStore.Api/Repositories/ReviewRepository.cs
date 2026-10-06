using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Review;
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
                .Include(r => r.Images)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<Review?> GetByIdAsync(int id)
            => await _context.Reviews.FindAsync(id);

        public async Task<Review?> GetByIdWithDetailsAsync(int id)
            => await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Review>> GetByProductIdAsync(int productId)
            => await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Images)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Review>> GetByCustomerIdAsync(int customerId)
            => await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.Images)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<Review?> GetByCustomerAndProductAsync(int customerId, int productId)
            => await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ProductId == productId);

        public async Task<bool> CustomerExistsAsync(int customerId)
            => await _context.Customers.AnyAsync(c => c.Id == customerId);

        public async Task<bool> ProductExistsAsync(int productId)
            => await _context.Products.AnyAsync(p => p.Id == productId);

        public async Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .AnyAsync(o => o.OrderItems.Any(oi => oi.ProductVariant != null && oi.ProductVariant.ProductId == productId));
        }

        public async Task<ReviewSummaryDto> GetReviewSummaryByProductIdAsync(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => r.Rating)
                .ToListAsync();

            var summary = new ReviewSummaryDto
            {
                ProductId = productId,
                TotalReviews = reviews.Count,
                AverageRating = reviews.Any() ? Math.Round(reviews.Average(), 1) : 0
            };

            foreach (var rating in reviews)
            {
                if (summary.RatingDistribution.ContainsKey(rating))
                {
                    summary.RatingDistribution[rating]++;
                }
            }

            return summary;
        }

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
