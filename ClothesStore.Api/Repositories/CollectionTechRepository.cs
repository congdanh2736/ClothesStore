using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class CollectionTechRepository : ICollectionTechRepository
    {
        public readonly ApplicationDbContext _context;
        public CollectionTechRepository (ApplicationDbContext context) => _context = context;
        public async Task<IEnumerable<CollectionTech>> GetAllAsync()
            => await _context.CollectionTechs
                .Include(c => c.ProductCollections)
                .ToListAsync();

        public async Task<IEnumerable<CollectionTech>> GetByTypeAsync(string type)
            => await _context.CollectionTechs
                .Where(c => c.Type == type)
                .Include(c => c.ProductCollections)
                .ToListAsync();

        public async Task<CollectionTech?> GetByIdAsync(int id)
            => await _context.CollectionTechs.FindAsync(id);

        public async Task<CollectionTech?> GetByIdWithDetailsAsync(int id)
            => await _context.CollectionTechs
                .Include(c => c.ProductCollections)
                .FirstOrDefaultAsync(c => c.Id == id);
        public async Task AddAsync(CollectionTech entity)
        {
            await _context.CollectionTechs.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(CollectionTech entity)
        {
            _context.CollectionTechs.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(CollectionTech entity)
        {
            _context.CollectionTechs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}