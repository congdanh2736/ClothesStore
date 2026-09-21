using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class StoreItemStatRepository : IStoreItemStatRepository
    {
        private readonly ApplicationDbContext _context;
        public StoreItemStatRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<StoreItemStat>> GetAllAsync() => await _context.StoreItemStats.Include(x => x.Store).Include(x => x.ProductVariant).ToListAsync();
        public async Task<StoreItemStat?> GetByIdAsync(int id) => await _context.StoreItemStats.FindAsync(id);
        public async Task<StoreItemStat?> GetByIdWithDetailsAsync(int id) => await _context.StoreItemStats.Include(x => x.Store).Include(x => x.ProductVariant).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<bool> StoreExistsAsync(int storeId) => await _context.Stores.AnyAsync(x => x.Id == storeId);
        public async Task<bool> ProductVariantExistsAsync(int variantId) => await _context.ProductVariants.AnyAsync(x => x.VariantId == variantId);
        public async Task<StoreItemStat?> GetByStoreVariantAndDateAsync(int storeId, int variantId, DateOnly statDate) => await _context.StoreItemStats.FirstOrDefaultAsync(x => x.StoreId == storeId && x.VariantId == variantId && x.StatDate == statDate);
        public async Task AddAsync(StoreItemStat entity) { await _context.StoreItemStats.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(StoreItemStat entity) { _context.StoreItemStats.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(StoreItemStat entity) { _context.StoreItemStats.Remove(entity); await _context.SaveChangesAsync(); }
    }
}
