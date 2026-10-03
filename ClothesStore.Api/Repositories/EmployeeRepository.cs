using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Employee>> GetAllAsync()
            => await _context.Employees
                .Include(e => e.Store)
                .ToListAsync();

        public async Task<Employee?> GetByIdAsync(int id)
            => await _context.Employees.FindAsync(id);

        public async Task<Employee?> GetByIdWithDetailsAsync(int id)
            => await _context.Employees
                .Include(e => e.Store)
                .FirstOrDefaultAsync(e => e.Id == id);

        public async Task<bool> StoreExistsAsync(int? storeId)
            => await _context.Stores.AnyAsync(s => s.Id == storeId);

        public async Task AddAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Employee employee)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }
    }
}
