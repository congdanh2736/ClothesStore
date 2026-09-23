using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ProductVariantRepository : IProductVariantRepository
    {
        public readonly ApplicationDbContext _context;
        public ProductVariantRepository(ApplicationDbContext context) => _context = context;
        public async Task<IEnumerable<ProductVariant>> GetAllAsync()
            => await _context.ProductVariants
                .Include(c => c.CartItems)
                .Include(c => c.StoreItemStats)
                .Include(c => c.StoreStocks)
                .Include(c => c.OrderItems)
                .ToListAsync();
        public async Task<ProductVariant?> GetByIdAsync(int id)
            => await _context.ProductVariants
                .FindAsync(id);
        public async Task<ProductVariant?> GetByIdWithDetailsAsync(int id)
            => await _context.ProductVariants
                .Include(c => c.CartItems)
                .Include(c => c.StoreItemStats)
                .Include(c => c.StoreStocks)
                .Include(c => c.OrderItems)
                .FirstOrDefaultAsync(c => c.VariantId == id);
        public async Task AddAsync(ProductVariant entity)
        {
            await _context.ProductVariants.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(ProductVariant entity)
        {
            _context.ProductVariants.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(ProductVariant entity)
        {
            _context.ProductVariants.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}