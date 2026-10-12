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
                .Include(c => c.Product)
                .ToListAsync();

        public async Task<ProductVariant?> GetByIdAsync(int id)
            => await _context.ProductVariants
                .FindAsync(id);

        public async Task<ProductVariant?> GetByIdWithDetailsAsync(int id)
            => await _context.ProductVariants
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.VariantId == id);

        public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId)
            => await _context.ProductVariants
                .Include(c => c.Product)
                .Where(c => c.ProductId == productId)
                .ToListAsync();

        public async Task<bool> ProductExistsAsync(int productId)
            => await _context.Products.AnyAsync(p => p.Id == productId);

        public async Task<bool> DuplicateExistsAsync(int productId, string? color, string? size, int? excludeVariantId = null)
            => await _context.ProductVariants.AnyAsync(v =>
                v.ProductId == productId && v.Color == color && v.Size == size
                && (excludeVariantId == null || v.VariantId != excludeVariantId));

        public async Task<bool> HasOrdersAsync(int variantId)
            => await _context.OrderItems.AnyAsync(oi => oi.VariantId == variantId);

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

        // Dọn các bảng tham chiếu Restrict (giỏ hàng, tồn kho, thống kê) trước khi xóa biến thể
        public async Task DeleteAsync(ProductVariant entity)
        {
            _context.CartItems.RemoveRange(_context.CartItems.Where(ci => ci.VariantId == entity.VariantId));
            _context.StoreStocks.RemoveRange(_context.StoreStocks.Where(s => s.VariantId == entity.VariantId));
            _context.StoreItemStats.RemoveRange(_context.StoreItemStats.Where(s => s.VariantId == entity.VariantId));

            _context.ProductVariants.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
