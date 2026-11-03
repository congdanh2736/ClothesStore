using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace ClothesStore.Api.Repositories
{
    public class CategoryRepository : ICategoryRepository 
    {
        // Biến lưu kết nối database
        private readonly ApplicationDbContext _context;

        //ngay khi được khởi tạo thì gán cho _context 1 kết nối database
        public CategoryRepository(ApplicationDbContext context) => _context = context;

        //Lấy danh sách đầy đủ tất cả dữ liệu
        /*
         * [SỬA - CHỐNG CHẬM / TIMEOUT] AsSplitQuery: có nhiều danh sách con được Include cùng lúc.
         * Nếu gộp chung 1 câu SQL JOIN thì số dòng = TÍCH số phần tử các danh sách (nhân chéo, "cartesian explosion"),
         * tách thành nhiều câu SQL nhỏ thì số dòng chỉ còn là TỔNG.
         * AsNoTracking (ở hàm lấy danh sách): chỉ đọc để hiển thị, không theo dõi thay đổi -> nhanh và nhẹ hơn.
         */
        public async Task<IEnumerable<Category>> GetAllAsync()
            => await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildrenCategories)
                .Include(c => c.Products)
                .Include(c => c.SizeCharts)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

        // tìm danh mục tương ứng với Id
        public async Task<Category?> GetByIdAsync(int id) 
            => await _context.Categories.FindAsync(id);


        // tìm danh mục tương ứng với Id, chứa toàn bộ thông tin của danh mục đó
        public async Task<Category?> GetByIdWithDetailsAsync(int id)
            => await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildrenCategories)
                .Include(c => c.Products)
                .Include(c => c.SizeCharts)
                .AsSplitQuery() // [SỬA - CHỐNG CHẬM] xem giải thích ở GetAllAsync
                .FirstOrDefaultAsync(c => c.Id == id);

        // thêm 1 danh mục mới
        public async Task AddAsync(Category entity)
        {
            await _context.Categories.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        // sửa 1 danh mục có sẵn
        public async Task UpdateAsync(Category entity)
        {
            _context.Categories.Update(entity);
            await _context.SaveChangesAsync();
        }

        // xóa 1 danh mục có sẵn
        public async Task DeleteAsync(Category entity)
        {
            _context.Categories.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}