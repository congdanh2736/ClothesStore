using AutoMapper;
using ClothesStore.Api.DTOs.ProductImage;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class ProductImageService : IProductImageService {
        private readonly IMapper _mapper;
        private readonly IProductImageRepository _repository;
        public ProductImageService(IProductImageRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        // Lấy toàn bộ danh sách Ảnh sản phẩm
        public async Task<IEnumerable<ProductImageDto>> GetAllAsync()
        {
            var ProductImage = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductImageDto>>(ProductImage);
        }

        // Lấy chi tiết 1 Ảnh sản phẩm theo Id
        public async Task<ProductImageDto?> GetByIdAsync(int id)
        {
            var ProductImage = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if (ProductImage == null)
                return null;

            return _mapper.Map<ProductImageDto>(ProductImage);
        }

        // Tạo mới Ảnh sản phẩm
        public async Task<(bool Success, string? Error, ProductImageDto? Data)> CreateAsync(CreateProductImageDto dto)
        {
            

            // Chuyển DTO sang Model để lưu vào database
            var ProductImage = _mapper.Map<ProductImage>(dto);
            await _repository.AddAsync(ProductImage);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<ProductImageDto>(ProductImage));
        }

        //Chỉnh sửa Ảnh sản phẩm
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductImageDto dto)
        {
            var ProductImage = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (ProductImage == null)
                return (false, "id của 'Ảnh sản phẩm' không tìm thấy");

            //Cập nhật thông tin 
            ProductImage.ImageUrl = dto.ImageUrl;
            ProductImage.ProductId = dto.ProductId;

            await _repository.UpdateAsync(ProductImage);
            return (true, null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            var ProductImage = await _repository.GetByIdAsync(id);

            if (ProductImage == null) return false;
            await _repository.DeleteAsync(ProductImage);
            return true;
        }
    }
}