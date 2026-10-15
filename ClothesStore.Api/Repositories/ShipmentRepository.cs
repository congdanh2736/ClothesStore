using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ShipmentRepository : IShipmentRepository
    {
        private readonly ApplicationDbContext _context;
        public ShipmentRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Shipment>> GetAllAsync()
            => await _context.Shipments
                .Include(s => s.Order)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

        public async Task<Shipment?> GetByIdAsync(int id)
            => await _context.Shipments.FindAsync(id);

        public async Task<Shipment?> GetByIdWithDetailsAsync(int id)
            => await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Shipment?> GetByOrderIdAsync(int orderId)
            => await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

        public async Task AddAsync(Shipment entity)
        {
            await _context.Shipments.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Shipment entity)
        {
            _context.Shipments.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Shipment entity)
        {
            _context.Shipments.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
