// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Interface tự động cập nhật Store_Daily_Stat, Store_Item_Stat theo vòng đời đơn hàng
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Services
{
    // Tự động cập nhật Store_Daily_Stat và Store_Item_Stat theo vòng đời đơn hàng
    public interface IStoreStatisticsService
    {
        Task RecordOrderPlacedAsync(Order order);
        Task RecordOrderCompletedAsync(Order order);
        Task RecordOrderCanceledAsync(Order order);
        Task RecordReturnAsync(int storeId, IEnumerable<StockLine> lines);
    }
}
