using ClothesStore.Api.DTOs.SizeChart;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface ISizeChartService : IService<SizeChartDto,CreateSizeChartDto,UpdateSizeChartDto>{
        // [THÊM] Khách tra cứu bảng size theo danh mục trước khi mua
        Task<IEnumerable<SizeChartDto>> GetByCategoryAsync(int categoryId);
    }
}
