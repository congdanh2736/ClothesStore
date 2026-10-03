using AutoMapper;
using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class ProductService : IProductService {
        private readonly IMapper _mapper;
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        // Lấy toàn bộ danh sách sản phẩm
        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var Product = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(Product);
        }

        // Lấy chi tiết 1 sản phẩm theo Id
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var Product = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if (Product == null)
                return null;

            return _mapper.Map<ProductDto>(Product);
        }

        // Tạo mới sản phẩm
        public async Task<(bool Success, string? Error, ProductDto? Data)> CreateAsync(CreateProductDto dto)
        {
            // Chuyển DTO sang Model để lưu vào database
            var Product = _mapper.Map<Product>(dto);
            await _repository.AddAsync(Product);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<ProductDto>(Product));
        }

        //Chỉnh sửa sản phẩm
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductDto dto)
        {
            var Product = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (Product == null)
                return (false, "id của 'sản phẩm' không tìm thấy");

            //Cập nhật thông tin 
            Product.Name = dto.Name;
            Product.CategoryId = dto.CategoryId;

            await _repository.UpdateAsync(Product);
            return (true, null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            var Product = await _repository.GetByIdAsync(id);

            if (Product == null) return false;
            await _repository.DeleteAsync(Product);
            return true;
        }
    }
}