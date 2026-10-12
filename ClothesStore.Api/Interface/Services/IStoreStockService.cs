using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IStoreStockService : IService<StoreStockDto, CreateStoreStockDto, UpdateStoreStockDto>
    {
        // [THÊM] Tồn kho của 1 cửa hàng
        Task<IEnumerable<StoreStockDto>> GetByStoreAsync(int storeId);
        // [THÊM] Tồn kho của 1 biến thể tại tất cả cửa hàng (hỗ trợ Click & Collect)
        Task<IEnumerable<StoreStockDto>> GetByVariantAsync(int variantId);
        // [THÊM] Nhập / xuất kho thủ công
        Task<StoreStockDto> AdjustAsync(AdjustStockDto dto);
    }
}
