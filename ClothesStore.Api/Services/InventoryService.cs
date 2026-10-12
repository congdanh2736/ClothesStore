// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Service tồn kho dùng chung cho OrderService, OrderItemService, CartService, ReturnRequestService
using ClothesStore.Api.Common;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    // Nghiệp vụ tồn kho: kiểm tra, trừ kho khi đặt hàng, hoàn kho khi hủy / trả hàng
    public class InventoryService : IInventoryService
    {
        private readonly IStoreStockRepository _stockRepository;

        public InventoryService(IStoreStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        public async Task<int> ResolveStoreAsync(int? storeId, IEnumerable<StockLine> lines)
        {
            var required = Merge(lines);
            if (required.Count == 0)
                throw new BusinessException("Đơn hàng phải có ít nhất một sản phẩm.");

            var stocks = (await _stockRepository.GetByVariantIdsAsync(required.Keys)).ToList();

            // Khách / nhân viên chỉ định cửa hàng -> kiểm tra cửa hàng đó
            if (storeId.HasValue)
            {
                if (!await _stockRepository.StoreExistsAsync(storeId.Value))
                    throw new NotFoundException("Cửa hàng không tồn tại.");

                foreach (var (variantId, quantity) in required)
                {
                    var available = stocks.FirstOrDefault(s => s.StoreId == storeId.Value && s.VariantId == variantId)?.Quantity ?? 0;
                    if (available < quantity)
                        throw new BusinessException(
                            $"Biến thể #{variantId} không đủ hàng tại cửa hàng đã chọn (còn {available}, cần {quantity}).");
                }
                return storeId.Value;
            }

            // Tự chọn cửa hàng đáp ứng được toàn bộ đơn (ưu tiên cửa hàng còn nhiều hàng nhất)
            var candidate = stocks
                .GroupBy(s => s.StoreId)
                .Where(g => required.All(r => g.Any(s => s.VariantId == r.Key && s.Quantity >= r.Value)))
                .OrderByDescending(g => g.Sum(s => s.Quantity))
                .Select(g => (int?)g.Key)
                .FirstOrDefault();

            if (candidate is null)
                throw new BusinessException("Không có cửa hàng nào còn đủ hàng cho toàn bộ sản phẩm trong đơn.");

            return candidate.Value;
        }

        public async Task DecreaseStockAsync(int storeId, IEnumerable<StockLine> lines)
        {
            foreach (var (variantId, quantity) in Merge(lines))
            {
                var stock = await _stockRepository.GetByStoreAndVariantAsync(storeId, variantId);
                if (stock is null || stock.Quantity < quantity)
                    throw new BusinessException(
                        $"Biến thể #{variantId} không đủ hàng tại cửa hàng (còn {stock?.Quantity ?? 0}, cần {quantity}).");

                stock.Quantity -= quantity;
                await _stockRepository.UpdateAsync(stock);
            }
        }

        public async Task IncreaseStockAsync(int storeId, IEnumerable<StockLine> lines)
        {
            foreach (var (variantId, quantity) in Merge(lines))
            {
                var stock = await _stockRepository.GetByStoreAndVariantAsync(storeId, variantId);
                if (stock is null)
                {
                    await _stockRepository.AddAsync(new StoreStock
                    {
                        StoreId = storeId,
                        VariantId = variantId,
                        Quantity = quantity
                    });
                }
                else
                {
                    stock.Quantity += quantity;
                    await _stockRepository.UpdateAsync(stock);
                }
            }
        }

        public async Task<int> GetTotalStockAsync(int variantId)
        {
            var stocks = await _stockRepository.GetByVariantIdsAsync(new[] { variantId });
            return stocks.Sum(s => s.Quantity);
        }

        // Gộp các dòng trùng variant, bỏ dòng số lượng <= 0
        private static Dictionary<int, int> Merge(IEnumerable<StockLine> lines)
            => lines
                .Where(l => l.Quantity > 0)
                .GroupBy(l => l.VariantId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    }
}
