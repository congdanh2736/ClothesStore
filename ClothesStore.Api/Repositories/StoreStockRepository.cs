using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class StoreStockRepository : IStoreStockRepository
    {
        private readonly ApplicationDbContext _context;
        public StoreStockRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<StoreStock>> GetAllAsync() => await _context.StoreStocks.Include(x => x.Store).Include(x => x.ProductVariant).ToListAsync();
        public async Task<StoreStock?> GetByIdAsync(int id) => await _context.StoreStocks.FindAsync(id);
        public async Task<StoreStock?> GetByIdWithDetailsAsync(int id) => await _context.StoreStocks.Include(x => x.Store).Include(x => x.ProductVariant).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<bool> StoreExistsAsync(int storeId) => await _context.Stores.AnyAsync(x => x.Id == storeId);
        public async Task<bool> ProductVariantExistsAsync(int variantId) => await _context.ProductVariants.AnyAsync(x => x.VariantId == variantId);
        public async Task<StoreStock?> GetByStoreAndVariantAsync(int storeId, int variantId) => await _context.StoreStocks.FirstOrDefaultAsync(x => x.StoreId == storeId && x.VariantId == variantId);
        public async Task AddAsync(StoreStock entity) { await _context.StoreStocks.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(StoreStock entity) { _context.StoreStocks.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(StoreStock entity) { _context.StoreStocks.Remove(entity); await _context.SaveChangesAsync(); }
    }
}
