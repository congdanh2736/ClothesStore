using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class MembershipTierRepository : IMembershipTierRepository
    {
        private readonly ApplicationDbContext _context;

        public MembershipTierRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MembershipTier>> GetAllAsync()
            => await _context.MembershipTiers
                .Include(mt => mt.Customers)
                .ToListAsync();

        public async Task<MembershipTier?> GetByIdAsync(int id)
            => await _context.MembershipTiers.FindAsync(id);

        public async Task<MembershipTier?> GetByIdWithDetailsAsync(int id)
            => await _context.MembershipTiers
                .Include(mt => mt.Customers)
                .FirstOrDefaultAsync(mt => mt.Id == id);

        public async Task AddAsync(MembershipTier membershipTier)
        {
            await _context.MembershipTiers.AddAsync(membershipTier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MembershipTier membershipTier)
        {
            _context.MembershipTiers.Update(membershipTier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(MembershipTier membershipTier)
        {
            _context.MembershipTiers.Remove(membershipTier);
            await _context.SaveChangesAsync();
        }
    }
}
