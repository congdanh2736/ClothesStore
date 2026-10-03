using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly ApplicationDbContext _context;
        public StoreRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Store>> GetAllAsync() => await _context.Stores
            .Include(s => s.StoreStocks).Include(s => s.StoreDailyStats).Include(s => s.StoreItemStats).ToListAsync();
        public async Task<Store?> GetByIdAsync(int id) => await _context.Stores.FindAsync(id);
        public async Task<Store?> GetByIdWithDetailsAsync(int id) => await _context.Stores
            .Include(s => s.StoreStocks).Include(s => s.StoreDailyStats).Include(s => s.StoreItemStats)
            .FirstOrDefaultAsync(s => s.Id == id);
        public async Task AddAsync(Store entity) { await _context.Stores.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Store entity) { _context.Stores.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Store entity) { _context.Stores.Remove(entity); await _context.SaveChangesAsync(); }
    }
}
