using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ProductRepository : IProductRepository 
    {
        public readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context) => _context = context;

        /*
         * [SỬA - CHỐNG CHẬM / TIMEOUT] AsSplitQuery: có nhiều danh sách con được Include cùng lúc.
         * Nếu gộp chung 1 câu SQL JOIN thì số dòng = TÍCH số phần tử các danh sách (nhân chéo, "cartesian explosion"),
         * tách thành nhiều câu SQL nhỏ thì số dòng chỉ còn là TỔNG.
         * AsNoTracking (ở hàm lấy danh sách): chỉ đọc để hiển thị, không theo dõi thay đổi -> nhanh và nhẹ hơn.
         */
        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _context.Products
                .Include(c => c.Category)
                .Include(c => c.ProductCollections)
                    .ThenInclude(pc => pc.CollectionTech)
                .Include(c => c.ProductImages)
                .Include(c => c.ProductVariants)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

        public async Task<Product?> GetByIdAsync(int id)
            => await _context.Products
                .FindAsync(id);

        public async Task<Product?> GetByIdWithDetailsAsync(int id)
            => await _context.Products
                .Include(c => c.Category)
                .Include(c => c.ProductCollections)
                    .ThenInclude(pc => pc.CollectionTech)
                .Include(c => c.ProductImages)
                .Include(c => c.ProductVariants)
                .AsSplitQuery() // [SỬA - CHỐNG CHẬM] xem giải thích ở GetAllAsync
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId)
            => await _context.Products
                .Include(c => c.Category)
                .Include(c => c.ProductImages)
                .Include(c => c.ProductVariants)
                .Where(c => c.CategoryId == categoryId)
                .AsSplitQuery() // [SỬA - CHỐNG CHẬM] ảnh x biến thể không còn bị nhân chéo
                .AsNoTracking()
                .ToListAsync();

        public async Task<bool> CategoryExistsAsync(int categoryId)
            => await _context.Categories.AnyAsync(c => c.Id == categoryId);

        public async Task<bool> HasOrdersAsync(int productId)
            => await _context.OrderItems.AnyAsync(oi => oi.ProductVariant != null && oi.ProductVariant.ProductId == productId);

        public async Task<bool> CollectionExistsAsync(int collectionId)
            => await _context.CollectionTechs.AnyAsync(c => c.Id == collectionId);

        public async Task<ProductCollection?> GetProductCollectionAsync(int productId, int collectionId)
            => await _context.ProductCollections
                .FirstOrDefaultAsync(pc => pc.ProductId == productId && pc.CollectionId == collectionId);

        public async Task AddProductCollectionAsync(ProductCollection productCollection)
        {
            await _context.ProductCollections.AddAsync(productCollection);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveProductCollectionAsync(ProductCollection productCollection)
        {
            _context.ProductCollections.Remove(productCollection);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(Product entity)
        {
            await _context.Products.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync( Product entity)
        {
            _context.Products.Update(entity);
            await _context.SaveChangesAsync();
        }

        /*
         * Xóa sản phẩm: các bảng tham chiếu tới Product/Variant đang để Restrict (Wishlist, CartItem, StoreStock, StoreItemStat)
         * nên phải dọn các dòng phụ thuộc trước, nếu không SQL Server sẽ báo lỗi khóa ngoại.
         * (Service đã chặn trường hợp sản phẩm đã có đơn hàng)
         */
        public async Task DeleteAsync(Product entity)
        {
            var variantIds = await _context.ProductVariants
                .Where(v => v.ProductId == entity.Id)
                .Select(v => v.VariantId)
                .ToListAsync();

            _context.Wishlists.RemoveRange(_context.Wishlists.Where(w => w.ProductId == entity.Id));
            _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.ProductId == entity.Id));
            _context.CartItems.RemoveRange(_context.CartItems.Where(ci => variantIds.Contains(ci.VariantId)));
            _context.StoreStocks.RemoveRange(_context.StoreStocks.Where(s => variantIds.Contains(s.VariantId)));
            _context.StoreItemStats.RemoveRange(_context.StoreItemStats.Where(s => variantIds.Contains(s.VariantId)));

            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
