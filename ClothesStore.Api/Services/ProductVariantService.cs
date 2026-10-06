using AutoMapper;
using ClothesStore.Api.DTOs.ProductVariant;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class ProductVariantService : IProductVariantService {
        private readonly IMapper _mapper;
        private readonly IProductVariantRepository _repository;

        public ProductVariantService(IProductVariantRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        // Lấy toàn bộ danh sách Phiên bản
        public async Task<IEnumerable<ProductVariantDto>> GetAllAsync()
        {
            var ProductVariant = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductVariantDto>>(ProductVariant);
        }

        // Lấy chi tiết 1 Phiên bản theo Id
        public async Task<ProductVariantDto?> GetByIdAsync(int id)
        {
            var ProductVariant = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if (ProductVariant == null)
                return null;

            return _mapper.Map<ProductVariantDto>(ProductVariant);
        }

        // Tạo mới Phiên bản
        public async Task<(bool Success, string? Error, ProductVariantDto? Data)> CreateAsync(CreateProductVariantDto dto)
        {
            // Chuyển DTO sang Model để lưu vào database
            var ProductVariant = _mapper.Map<ProductVariant>(dto);
            await _repository.AddAsync(ProductVariant);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<ProductVariantDto>(ProductVariant));
        }

        //Chỉnh sửa Phiên bản
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductVariantDto dto)
        {
            var ProductVariant = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (ProductVariant == null)
                return (false, "id của 'biến thể sản phẩm' không tìm thấy");

            //Cập nhật thông tin 
            ProductVariant.Color = dto.Color;
            ProductVariant.Size = dto.Size;
            ProductVariant.Price = dto.Price;

            await _repository.UpdateAsync(ProductVariant);
            return (true, null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            var ProductVariant = await _repository.GetByIdAsync(id);

            if (ProductVariant == null) return false;
            await _repository.DeleteAsync(ProductVariant);
            return true;
        }
    }
}