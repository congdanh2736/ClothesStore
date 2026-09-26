using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ProductImageRepository : IProductImageRepository
    {
        public readonly ApplicationDbContext _context;
        public ProductImageRepository(ApplicationDbContext context) 
            => _context = context;
        public async Task<IEnumerable<ProductImage>> GetAllAsync()
            => await _context.ProductImages
                .Include(c => c.Product)
                .ToListAsync();
        public async Task<ProductImage?> GetByIdAsync(int id)
            => await _context.ProductImages
                .FindAsync(id);
        public async Task<ProductImage?> GetByIdWithDetailsAsync(int id)
            => await _context.ProductImages
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CollectionId == id);
        public async Task AddAsync(ProductImage entity)
        {
            await _context.ProductImages.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(ProductImage entity)
        {
            _context.ProductImages.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(ProductImage entity)
        {
            _context.ProductImages.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}