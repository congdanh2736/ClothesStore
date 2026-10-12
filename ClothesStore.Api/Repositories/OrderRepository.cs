using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderRepository(ApplicationDbContext context) { _context = context; }

        //Add new Order
        public async Task AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        //Delete Order
        public async Task DeleteAsync(Order order)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }

        // Get Order List
        public async Task<IEnumerable<Order>> GetAllAsync() 
            => await WithDetails()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

        public async Task<Order?> GetByIdAsync(int id)
            => await _context.Orders.FindAsync(id);

        public async Task<Order?> GetByIdWithDetailsAsync(int id)
            => await WithDetails().FirstOrDefaultAsync(order => order.Id == id);

        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
            => await WithDetails()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

        //Update Order
        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        private IQueryable<Order> WithDetails()
            => _context.Orders
                .Include(order => order.Address)
                .Include(order => order.Customer)
                .Include(order => order.Promotion)
                .Include(order => order.Store)
                .Include(order => order.OrderItems)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(variant => variant!.Product)
                .Include(order => order.PaymentTransaction)
                .Include(order => order.Shipment)
                .AsSplitQuery();
    }
}
