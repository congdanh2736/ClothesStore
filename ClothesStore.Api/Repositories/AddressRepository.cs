using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public AddressRepository(ApplicationDbContext context) => _context = context;

        //  Phương thức lay tất cả các địa chỉ từ cơ sở dữ liệu, bao gồm thông tin về khách hàng và các đơn hàng liên quan.
        public async Task<IEnumerable<Address>> GetAllAsync()
            => await _context.Addresses
                .Include(a => a.Customer)
                .Include(a => a.Orders)
                .ToListAsync();

        // Phương thức lay một địa chỉ cụ thể từ cơ sở dữ liệu dựa trên ID của nó.
        public async Task<Address?> GetByIdAsync(int id)
            => await _context.Addresses.FindAsync(id);

        // Phương thức lay một địa chỉ cụ thể từ cơ sở dữ liệu dựa trên ID của nó, bao gồm thông tin về khách hàng và các đơn hàng liên quan.
        public async Task<Address?> GetByIdWithDetailsAsync(int id)
            => await _context.Addresses
                .Include(a => a.Customer)
                .Include(a => a.Orders)
                .FirstOrDefaultAsync(a => a.Id == id);


        /*
         * Kiểm tra xem khách hàng có tồn tại trong cơ sở dữ liệu hay không dựa trên ID của khách hàng.
         * Sau khi kiểm tra xong thì ta biết được rằng địa chỉ này có hợp lệ hay không, vì địa chỉ phải thuộc về một khách hàng nào đó.
         */
        public async Task<bool> CustomerExistsAsync(int? customerId)
            => await _context.Customers.AnyAsync(c => c.Id == customerId);


        public async Task AddAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Address address)
        {
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Address address)
        {
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
        }
    }
}
