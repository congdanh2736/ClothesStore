using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class LoyaltyTransactionRepository : ILoyaltyTransactionRepository
    {
        private readonly ApplicationDbContext _context;
        public LoyaltyTransactionRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<LoyaltyTransaction>> GetAllAsync()
            => await _context.LoyaltyTransactions
                .OrderByDescending(lt => lt.TxnDate)
                .ToListAsync();

        public async Task<LoyaltyTransaction?> GetByIdAsync(int id)
            => await _context.LoyaltyTransactions.FindAsync(id);

        public async Task<LoyaltyTransaction?> GetByIdWithDetailsAsync(int id)
            => await _context.LoyaltyTransactions
                .Include(lt => lt.Customer)
                .FirstOrDefaultAsync(lt => lt.Id == id);

        public async Task<IEnumerable<LoyaltyTransaction>> GetByCustomerIdAsync(int customerId)
            => await _context.LoyaltyTransactions
                .Where(lt => lt.CustomerId == customerId)
                .OrderByDescending(lt => lt.TxnDate)
                .ToListAsync();

        public async Task<int> GetTotalPointsAsync(int customerId)
            => await _context.LoyaltyTransactions
                .Where(lt => lt.CustomerId == customerId)
                .SumAsync(lt => (int?)lt.PointsChange) ?? 0;

        public async Task<bool> CustomerExistsAsync(int customerId)
            => await _context.Customers.AnyAsync(c => c.Id == customerId);

        public async Task AddAsync(LoyaltyTransaction entity)
        {
            await _context.LoyaltyTransactions.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LoyaltyTransaction entity)
        {
            _context.LoyaltyTransactions.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(LoyaltyTransaction entity)
        {
            _context.LoyaltyTransactions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
