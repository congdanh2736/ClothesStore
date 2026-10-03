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
    public class OrderItemRepository : IOrderItemRepository
    {
        public readonly ApplicationDbContext _context;
        public OrderItemRepository( ApplicationDbContext context)
        {
            _context = context ;
        }
        public async Task AddAsync(OrderItem orderItem)
        {
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(OrderItem orderItem)
        {
            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
        }
        // Get OrderItem list with navigations entity
        public async Task<IEnumerable<OrderItem>> GetAllAsync()
            => await _context.OrderItems
                .Include( o => o.Order)
                .Include( o => o.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
                .ToArrayAsync();

        public async Task<OrderItem?> GetByIdAsync(int id)
            => await _context.OrderItems.FindAsync(id);

        public async Task<OrderItem?> GetByIdWithDetailsAsync(int id)
            => await _context.OrderItems
                .Include(item => item.Order)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
                .FirstOrDefaultAsync(item => item.Id == id);

        public async Task UpdateAsync(OrderItem orderItem)
        {
           _context.OrderItems.Update(orderItem);
           await _context.SaveChangesAsync();
        }
    }
}
