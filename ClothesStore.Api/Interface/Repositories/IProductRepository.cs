using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
        Task<bool> CategoryExistsAsync(int categoryId);
        // Sản phẩm đã phát sinh đơn hàng thì không được xóa
        Task<bool> HasOrdersAsync(int productId);

        // Bảng trung gian Product_Collection (bảng 9)
        Task<bool> CollectionExistsAsync(int collectionId);
        Task<ProductCollection?> GetProductCollectionAsync(int productId, int collectionId);
        Task AddProductCollectionAsync(ProductCollection productCollection);
        Task RemoveProductCollectionAsync(ProductCollection productCollection);
    }
}
