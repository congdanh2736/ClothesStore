    using ClothesStore.Api.Data;
    using ClothesStore.Api.Models;
    using Microsoft.EntityFrameworkCore;
    using ClothesStore.Api.Interface.Repositories;

    namespace ClothesStore.Api.Repositories
    {
        public class CustomerRepository : ICustomerRepository
        {
            private readonly ApplicationDbContext _context;
            public CustomerRepository(ApplicationDbContext context) => _context = context;

            public async Task<IEnumerable<Customer>> GetAllAsync()
                => await _context.Customers
                    .Include(c => c.MembershipTier)
                    .Include(c => c.Addresses)
                    .Include(c => c.Wishlists)
                    .Include(c => c.LoyaltyTransactions)
                    .AsSplitQuery()
                    .ToListAsync();

            public async Task<Customer?> GetByIdAsync(int id)
                => await _context.Customers.FindAsync(id);

            public async Task<Customer?> GetByIdWithDetailsAsync(int id)
                => await _context.Customers
                    .Include(c => c.MembershipTier)
                    .Include(c => c.Addresses)
                    .Include(c => c.Wishlists)
                    .Include(c => c.LoyaltyTransactions)
                    .FirstOrDefaultAsync(c => c.Id == id);

            public async Task<bool> ApplicationUserExistsAsync(string applicationUserId)
                => await _context.Users.AnyAsync(u => u.Id == applicationUserId);

            public async Task<bool> MembershipTierExistsAsync(int tierId)
                => await _context.MembershipTiers.AnyAsync(t => t.Id == tierId);

            public async Task<Customer?> GetByApplicationUserIdAsync(string applicationUserId)
                => await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

            public async Task<bool> HasOrdersAsync(int customerId)
                => await _context.Orders.AnyAsync(o => o.CustomerId == customerId);

            public async Task AddAsync(Customer customer)
            {
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
            }

            public async Task UpdateAsync(Customer customer)
            {
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(Customer customer)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }
    }
