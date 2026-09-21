using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class StoreDailyStatRepository : IStoreDailyStatRepository
    {
        private readonly ApplicationDbContext _context;
        public StoreDailyStatRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<StoreDailyStat>> GetAllAsync() => await _context.StoreDailyStats.Include(x => x.Store).ToListAsync();
        public async Task<StoreDailyStat?> GetByIdAsync(int id) => await _context.StoreDailyStats.FindAsync(id);
        public async Task<StoreDailyStat?> GetByIdWithDetailsAsync(int id) => await _context.StoreDailyStats.Include(x => x.Store).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<bool> StoreExistsAsync(int storeId) => await _context.Stores.AnyAsync(x => x.Id == storeId);
        public async Task<StoreDailyStat?> GetByStoreAndDateAsync(int storeId, DateOnly statDate) => await _context.StoreDailyStats.FirstOrDefaultAsync(x => x.StoreId == storeId && x.StatDate == statDate);
        public async Task AddAsync(StoreDailyStat entity) { await _context.StoreDailyStats.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(StoreDailyStat entity) { _context.StoreDailyStats.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(StoreDailyStat entity) { _context.StoreDailyStats.Remove(entity); await _context.SaveChangesAsync(); }
    }
}
