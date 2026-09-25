using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class SizeChartRepository : ISizeChartRepository 
    {
        public ApplicationDbContext _context;
        public SizeChartRepository(ApplicationDbContext context) => _context = context; 
        public async Task<IEnumerable<SizeChart>> GetAllAsync()
            => await _context.sizeCharts
                .Include(c => c.Category)
                .ToListAsync();
        public async Task<SizeChart?> GetByIdAsync(int id)
            => await _context.sizeCharts
                .FindAsync(id);
        public async Task<SizeChart?> GetByIdWithDetailsAsync(int id)
            => await _context.sizeCharts
                .Include(x => x.Category)
                .FirstAsync(x => x.Id == id);
        public async Task AddAsync(SizeChart entity)
        {
            await _context.sizeCharts.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(SizeChart entity)
        {
            _context.sizeCharts.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(SizeChart entity)
        {
            _context.sizeCharts.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}