using AutoMapper;
using ClothesStore.Api.DTOs.SizeChart;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class SizeChartService :ISizeChartService {
        private readonly IMapper _mapper;
        private readonly ISizeChartRepository _repository;

        public SizeChartService(ISizeChartRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        // Lấy toàn bộ danh sách Bảng size
        public async Task<IEnumerable<SizeChartDto>> GetAllAsync()
        {
            var sizeCharts = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<SizeChartDto>>(sizeCharts);
        }

        // Lấy chi tiết 1 Bảng size theo Id
        public async Task<SizeChartDto?> GetByIdAsync(int id)
        {
            var sizeCharts = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if (sizeCharts == null)
                return null;

            return _mapper.Map<SizeChartDto>(sizeCharts);
        }

        // Tạo mới Bảng size
        public async Task<(bool Success, string? Error, SizeChartDto? Data)> CreateAsync(CreateSizeChartDto dto)
        {
            // Chuyển DTO sang Model để lưu vào database
            var sizeCharts = _mapper.Map<SizeChart>(dto);
            await _repository.AddAsync(sizeCharts);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<SizeChartDto>(sizeCharts));
        }

        //Chỉnh sửa Bảng size
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateSizeChartDto dto)
        {
            var sizeCharts = await _repository.GetByIdAsync(id);
            //kiểm tra bảng size có tồn tại hay không
            if (sizeCharts == null)
                return (false, "id của 'Bảng size' không tìm thấy");

            //Cập nhật thông tin 
            sizeCharts.SizeLabel = dto.SizeLabel ?? sizeCharts.SizeLabel;
            sizeCharts.Measurements = dto.Measurements ?? sizeCharts.Measurements;
            sizeCharts.CategoryId = dto.CategoryId;

            await _repository.UpdateAsync(sizeCharts);
            return (true, null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            var sizeCharts = await _repository.GetByIdAsync(id);

            if (sizeCharts == null) return false;
            await _repository.DeleteAsync(sizeCharts);
            return true;
        }
    }
}