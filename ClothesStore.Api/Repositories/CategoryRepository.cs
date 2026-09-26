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
        public async Task<IEnumerable<Category>> GetAllAsync()
            => await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildrenCategories)
                .Include(c => c.Products)
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
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
            => await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.ChildrenCategories)
                .Include(c => c.Products)
                .ToListAsync();

        public async Task<bool> CategoryExistsAsync(int categoryId)
            => await _context.Categories.AnyAsync(c => c.Id == categoryId);

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