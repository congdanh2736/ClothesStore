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
            => await _context.SizeCharts
                .Include(c => c.Category)
                .ToListAsync();

        public async Task<SizeChart?> GetByIdAsync(int id)
            => await _context.SizeCharts
                .FindAsync(id);

        public async Task<SizeChart?> GetByIdWithDetailsAsync(int id)
            => await _context.SizeCharts
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IEnumerable<SizeChart>> GetByCategoryIdAsync(int categoryId)
            => await _context.SizeCharts
                .Include(x => x.Category)
                .Where(x => x.CategoryId == categoryId)
                .ToListAsync();

        public async Task<bool> CategoryExistsAsync(int categoryId)
            => await _context.Categories.AnyAsync(c => c.Id == categoryId);

        public async Task AddAsync(SizeChart entity)
        {
            await _context.SizeCharts.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SizeChart entity)
        {
            _context.SizeCharts.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(SizeChart entity)
        {
            _context.SizeCharts.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
