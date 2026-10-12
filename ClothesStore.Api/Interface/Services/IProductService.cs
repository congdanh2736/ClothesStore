using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IProductService : IService<ProductDto,CreateProductDto,UpdateProductDto>{
        // [THÊM] Chi tiết sản phẩm: ảnh, biến thể (màu/size/giá), bộ sưu tập
        Task<ProductDetailDto?> GetDetailAsync(int id);
        // [THÊM] Lọc sản phẩm theo danh mục
        Task<IEnumerable<ProductDto>> GetByCategoryAsync(int categoryId);

        // [THÊM] Gắn / gỡ sản phẩm với bộ sưu tập - công nghệ (bảng Product_Collection)
        Task<(bool Success, string? Error)> AddToCollectionAsync(int productId, int collectionId);
        Task<(bool Success, string? Error)> RemoveFromCollectionAsync(int productId, int collectionId);
    }
}
