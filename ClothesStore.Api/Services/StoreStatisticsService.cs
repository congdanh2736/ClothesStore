// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Service tự động ghi thống kê cửa hàng (trước đây Store_Daily_Stat / Store_Item_Stat chỉ nhập tay)
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    /*
     * Tự động cập nhật số liệu thống kê theo vòng đời đơn hàng:
     * - Đặt đơn       : Store_Daily_Stat.TotalOrders + 1
     * - Hoàn tất đơn  : CompletedOrders + 1, TotalRevenue, TotalItemsSold; Store_Item_Stat.QuantitySold, TotalRevenue
     * - Hủy đơn       : CanceledOrders + 1
     * - Trả hàng      : Store_Item_Stat.ReturnQuantity
     * Chỉ ghi khi đơn có StoreId (cửa hàng xuất kho).
     */
    public class StoreStatisticsService : IStoreStatisticsService
    {
        private readonly IStoreDailyStatRepository _dailyRepository;
        private readonly IStoreItemStatRepository _itemRepository;

        public StoreStatisticsService(IStoreDailyStatRepository dailyRepository, IStoreItemStatRepository itemRepository)
        {
            _dailyRepository = dailyRepository;
            _itemRepository = itemRepository;
        }

        // Thống kê theo ngày làm việc của cửa hàng (giờ địa phương của server)
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        public async Task RecordOrderPlacedAsync(Order order)
        {
            if (order.StoreId is null) return;

            var daily = await GetOrCreateDailyAsync(order.StoreId.Value, Today);
            daily.TotalOrders += 1;
            await _dailyRepository.UpdateAsync(daily);
        }

        public async Task RecordOrderCompletedAsync(Order order)
        {
            if (order.StoreId is null) return;
            var storeId = order.StoreId.Value;

            var daily = await GetOrCreateDailyAsync(storeId, Today);
            daily.CompletedOrders += 1;
            daily.TotalRevenue += order.TotalAmount;
            daily.TotalItemsSold += order.OrderItems.Sum(i => i.Quantity);
            // [SỬA] Đơn đặt từ hôm trước nhưng hôm nay mới hoàn tất -> ngày hôm nay có thể có Completed > Total.
            //       Nâng TotalOrders lên để luôn đúng ràng buộc Completed + Canceled <= Total (giống lúc hủy đơn bên dưới).
            if (daily.CompletedOrders + daily.CanceledOrders > daily.TotalOrders)
                daily.TotalOrders = daily.CompletedOrders + daily.CanceledOrders;
            await _dailyRepository.UpdateAsync(daily);

            foreach (var group in order.OrderItems.GroupBy(i => i.VariantId))
            {
                var itemStat = await GetOrCreateItemAsync(storeId, group.Key, Today);
                itemStat.QuantitySold += group.Sum(i => i.Quantity);
                itemStat.TotalRevenue += group.Sum(i => i.Price * i.Quantity);
                await _itemRepository.UpdateAsync(itemStat);
            }
        }

        public async Task RecordOrderCanceledAsync(Order order)
        {
            if (order.StoreId is null) return;

            var daily = await GetOrCreateDailyAsync(order.StoreId.Value, Today);
            daily.CanceledOrders += 1;
            // Đảm bảo không vi phạm ràng buộc Completed + Canceled <= Total (đơn đặt từ hôm trước, hủy hôm nay)
            if (daily.CompletedOrders + daily.CanceledOrders > daily.TotalOrders)
                daily.TotalOrders = daily.CompletedOrders + daily.CanceledOrders;
            await _dailyRepository.UpdateAsync(daily);
        }

        public async Task RecordReturnAsync(int storeId, IEnumerable<StockLine> lines)
        {
            foreach (var group in lines.GroupBy(l => l.VariantId))
            {
                var itemStat = await GetOrCreateItemAsync(storeId, group.Key, Today);
                itemStat.ReturnQuantity += group.Sum(l => l.Quantity);
                await _itemRepository.UpdateAsync(itemStat);
            }
        }

        private async Task<StoreDailyStat> GetOrCreateDailyAsync(int storeId, DateOnly date)
        {
            var stat = await _dailyRepository.GetByStoreAndDateAsync(storeId, date);
            if (stat is not null) return stat;

            stat = new StoreDailyStat { StoreId = storeId, StatDate = date };
            await _dailyRepository.AddAsync(stat);
            return stat;
        }

        private async Task<StoreItemStat> GetOrCreateItemAsync(int storeId, int variantId, DateOnly date)
        {
            var stat = await _itemRepository.GetByStoreVariantAndDateAsync(storeId, variantId, date);
            if (stat is not null) return stat;

            stat = new StoreItemStat { StoreId = storeId, VariantId = variantId, StatDate = date };
            await _itemRepository.AddAsync(stat);
            return stat;
        }
    }
}
