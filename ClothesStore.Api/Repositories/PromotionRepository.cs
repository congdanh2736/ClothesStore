using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace ClothesStore.Api.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        public readonly ApplicationDbContext _context;
        public PromotionRepository( ApplicationDbContext context) => _context = context;
        public async Task AddAsync(Promotion promotion)
        {
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Promotion promotion)
        {
            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Promotion>> GetAllAsync()
            => await _context.Promotions
                .Include( p => p.Orders)
                .ToListAsync();
               

        public async Task<Promotion?> GetByIdAsync(int id)
            => await _context.Promotions.FindAsync(id);

        public async Task<Promotion?> GetByIdWithDetailsAsync(int id)
            => await _context.Promotions
                .Include(promotion => promotion.Orders)
                .FirstOrDefaultAsync(promotion => promotion.Id == id);

        public async Task UpdateAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }
    }
}
