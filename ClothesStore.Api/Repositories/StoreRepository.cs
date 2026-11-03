using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly ApplicationDbContext _context;
        public StoreRepository(ApplicationDbContext context) => _context = context;

        /*
         * [SỬA LỖI] Lỗi "Execution Timeout Expired" khi gọi GET /api/Store.
         * Trước đây lấy danh sách cửa hàng có kèm .Include() 3 danh sách con: tồn kho (StoreStocks),
         * thống kê ngày (StoreDailyStats), thống kê sản phẩm (StoreItemStats).
         * EF Core gộp cả 3 vào MỘT câu SQL JOIN -> số dòng = tồn kho x thống kê ngày x thống kê sản phẩm
         * của từng cửa hàng ("cartesian explosion"). Riêng Kho Online có hàng chục nghìn dòng tồn kho
         * -> câu SQL trả về hàng trăm triệu dòng -> quá 30 giây thì SQL Server báo timeout.
         * Danh sách cửa hàng (StoreDto) chỉ cần Id, Name, Location nên KHÔNG cần Include gì cả.
         * AsNoTracking: chỉ đọc, không theo dõi thay đổi -> nhanh và nhẹ hơn.
         */
        public async Task<IEnumerable<Store>> GetAllAsync() => await _context.Stores.AsNoTracking().ToListAsync();
        public async Task<Store?> GetByIdAsync(int id) => await _context.Stores.FindAsync(id);
        /*
         * [SỬA LỖI] Cùng lỗi như trên khi xem 1 cửa hàng (nhất là Kho Online).
         * AsSplitQuery: tách thành 4 câu SQL riêng (cửa hàng, tồn kho, thống kê ngày, thống kê sản phẩm)
         * thay vì 1 câu JOIN nhân chéo -> số dòng chỉ còn là TỔNG chứ không phải TÍCH.
         */
        public async Task<Store?> GetByIdWithDetailsAsync(int id) => await _context.Stores
            .Include(s => s.StoreStocks).Include(s => s.StoreDailyStats).Include(s => s.StoreItemStats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);
        public async Task<bool> HasOrdersAsync(int storeId) => await _context.Orders.AnyAsync(o => o.StoreId == storeId);
        public async Task AddAsync(Store entity) { await _context.Stores.AddAsync(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Store entity) { _context.Stores.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Store entity) { _context.Stores.Remove(entity); await _context.SaveChangesAsync(); }
    }
}