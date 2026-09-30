using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            => await _context.Orders
                .Include( o => o.Address)
                .Include( o => o.Customer)
                .Include( o => o.Promotion)
                .Include( o => o.OrderItems)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(variant => variant!.Product)
                .Include( o => o.PaymentTransaction)
                .ToListAsync();
                

        public async Task<Order?> GetByIdAsync(int id)
            => await _context.Orders.FindAsync(id);

        public async Task<Order?> GetByIdWithDetailsAsync(int id)
            => await _context.Orders
                .Include(order => order.Address)
                .Include(order => order.Customer)
                .Include(order => order.Promotion)
                .Include(order => order.OrderItems)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(variant => variant!.Product)
                .Include(order => order.PaymentTransaction)
                .FirstOrDefaultAsync(order => order.Id == id);

        //Update Order
        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}
