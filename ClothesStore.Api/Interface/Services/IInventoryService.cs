// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Interface nghiệp vụ tồn kho: kiểm tra / trừ / hoàn Store_Stock khi đặt, hủy, trả hàng
namespace ClothesStore.Api.Interface.Services
{
    // Một dòng hàng cần kiểm tra / trừ / hoàn kho
    public record StockLine(int VariantId, int Quantity);

    // Nghiệp vụ tồn kho theo cửa hàng (Store_Stock)
    public interface IInventoryService
    {
        // Kiểm tra cửa hàng có đủ hàng; nếu storeId = null thì tự chọn cửa hàng đủ hàng cho toàn bộ đơn
        Task<int> ResolveStoreAsync(int? storeId, IEnumerable<StockLine> lines);
        Task DecreaseStockAsync(int storeId, IEnumerable<StockLine> lines);
        Task IncreaseStockAsync(int storeId, IEnumerable<StockLine> lines);
        // Tổng tồn kho của 1 biến thể ở tất cả cửa hàng
        Task<int> GetTotalStockAsync(int variantId);
    }
}
