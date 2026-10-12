using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public ReturnRequestRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<ReturnRequest>> GetAllAsync()
            => await WithDetails()
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

        public async Task<ReturnRequest?> GetByIdAsync(int id)
            => await _context.ReturnRequests.FindAsync(id);

        public async Task<ReturnRequest?> GetByIdWithDetailsAsync(int id)
            => await WithDetails().FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<ReturnRequest>> GetByOrderIdAsync(int orderId)
            => await WithDetails()
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

        public async Task<IEnumerable<ReturnRequest>> GetByCustomerIdAsync(int customerId)
            => await WithDetails()
                .Where(r => r.Order != null && r.Order.CustomerId == customerId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

        public async Task<Dictionary<int, int>> GetReturnedQuantitiesAsync(int orderId, int? excludeReturnRequestId = null)
            => await _context.ReturnItems
                .Where(ri => ri.ReturnRequest != null
                    && ri.ReturnRequest.OrderId == orderId
                    && ri.ReturnRequest.Status != "Rejected"
                    && (excludeReturnRequestId == null || ri.ReturnRequestId != excludeReturnRequestId))
                .GroupBy(ri => ri.OrderItemId)
                .Select(g => new { OrderItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.OrderItemId, x => x.Quantity);

        public async Task AddAsync(ReturnRequest entity)
        {
            await _context.ReturnRequests.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ReturnRequest entity)
        {
            _context.ReturnRequests.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ReturnRequest entity)
        {
            _context.ReturnRequests.Remove(entity);
            await _context.SaveChangesAsync();
        }

        private IQueryable<ReturnRequest> WithDetails()
            => _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.OrderItem)
                        .ThenInclude(oi => oi!.ProductVariant)
                            .ThenInclude(v => v!.Product);
    }
}
