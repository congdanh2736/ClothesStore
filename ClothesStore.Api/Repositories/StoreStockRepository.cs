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

        // [SỬA - CHỐNG CHẬM] ~21.000 dòng chỉ để hiển thị -> AsNoTracking (không theo dõi thay đổi) cho nhẹ
        public async Task<IEnumerable<StoreStock>> GetAllAsync() => await _context.StoreStocks.Include(x => x.Store).Include(x => x.ProductVariant).ThenInclude(v => v!.Product).AsNoTracking().ToListAsync();
        public async Task<StoreStock?> GetByIdAsync(int id) => await _context.StoreStocks.FindAsync(id);
        public async Task<StoreStock?> GetByIdWithDetailsAsync(int id) => await _context.StoreStocks.Include(x => x.Store).Include(x => x.ProductVariant).ThenInclude(v => v!.Product).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<bool> StoreExistsAsync(int storeId) => await _context.Stores.AnyAsync(x => x.Id == storeId);
        public async Task<bool> ProductVariantExistsAsync(int variantId) => await _context.ProductVariants.AnyAsync(x => x.VariantId == variantId);
        public async Task<StoreStock?> GetByStoreAndVariantAsync(int storeId, int variantId) => await _context.StoreStocks.FirstOrDefaultAsync(x => x.StoreId == storeId && x.VariantId == variantId);

        public async Task<IEnumerable<StoreStock>> GetByStoreIdAsync(int storeId)
            => await _context.StoreStocks
                .Include(x => x.Store)
                .Include(x => x.ProductVariant)
                    .ThenInclude(v => v!.Product)
                .Where(x => x.StoreId == storeId)
                .AsNoTracking() // [SỬA - CHỐNG CHẬM] Kho Online có ~17.000 dòng, chỉ để hiển thị
                .ToListAsync();

        public async Task<IEnumerable<StoreStock>> GetByVariantIdsAsync(IEnumerable<int> variantIds)
        {
            var ids = variantIds.Distinct().ToList();
            return await _context.StoreStocks
                .Include(x => x.Store)
                .Include(x => x.ProductVariant)
                    .ThenInclude(v => v!.Product)
                .Where(x => ids.Contains(x.VariantId))
                .ToListAsync();
        }

        public async Task AddAsync(StoreStock entity) { await _context.StoreStocks.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(StoreStock entity) { _context.StoreStocks.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(StoreStock entity) { _context.StoreStocks.Remove(entity); await _context.SaveChangesAsync(); }
    }
}
