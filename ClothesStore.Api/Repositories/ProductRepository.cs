using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ProductRepository : IProductRepository 
    {
        public readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context) => _context = context;
        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _context.Products
                .Include(c => c.Category)
                .Include(c => c.ProductCollections)
                .Include(c => c.ProductImages)
                .Include(c => c.ProductVariants)
                .ToListAsync();
        public async Task<Product?> GetByIdAsync(int id)
            => await _context.Products
                .FindAsync(id);
        public async Task<Product?> GetByIdWithDetailsAsync(int id)
            => await _context.Products
                .Include(c => c.Category)
                .Include(c => c.ProductCollections)
                .Include(c => c.ProductImages)
                .Include(c => c.ProductVariants)
                .FirstOrDefaultAsync(c => c.Id == id);
        public async Task AddAsync(Product entity)
        {
            await _context.Products.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync( Product entity)
        {
            _context.Products.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Product entity)
        {
            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}