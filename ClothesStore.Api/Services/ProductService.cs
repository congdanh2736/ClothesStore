using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException khi chặn xóa sản phẩm
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

        // [THÊM] Chi tiết sản phẩm: ảnh, các biến thể (màu/size/giá), bộ sưu tập - công nghệ
        //        (trước đây đã có ProductDetailDto + mapping nhưng không có hàm/endpoint nào dùng)
        public async Task<ProductDetailDto?> GetDetailAsync(int id)
        {
            var product = await _repository.GetByIdWithDetailsAsync(id);
            return product == null ? null : _mapper.Map<ProductDetailDto>(product);
        }

        // [THÊM] Lọc sản phẩm theo danh mục
        public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(int categoryId)
        {
            var products = await _repository.GetByCategoryIdAsync(categoryId);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        // Tạo mới sản phẩm
        public async Task<(bool Success, string? Error, ProductDto? Data)> CreateAsync(CreateProductDto dto)
        {
            // [THÊM] Kiểm tra danh mục tồn tại (trước đây CategoryId sai -> lỗi khóa ngoại 500)
            if (!await _repository.CategoryExistsAsync(dto.CategoryId))
                return (false, "Danh mục không tồn tại.", null);

            // Chuyển DTO sang Model để lưu vào database
            var Product = _mapper.Map<Product>(dto);
            await _repository.AddAsync(Product);

            // [SỬA] Load lại kèm Category để DTO trả về có CategoryName
            var created = await _repository.GetByIdWithDetailsAsync(Product.Id);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<ProductDto>(created));
        }

        //Chỉnh sửa sản phẩm
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductDto dto)
        {
            var Product = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (Product == null)
                return (false, "id của 'sản phẩm' không tìm thấy");

            // [THÊM] Kiểm tra danh mục mới có tồn tại không
            if (!await _repository.CategoryExistsAsync(dto.CategoryId))
                return (false, "Danh mục không tồn tại.");

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

            // [THÊM] Sản phẩm đã có trong đơn hàng thì không được xóa (giữ lịch sử bán hàng)
            if (await _repository.HasOrdersAsync(id))
                throw new BusinessException("Sản phẩm đã phát sinh đơn hàng, không thể xóa.");

            // [SỬA] Repository.DeleteAsync giờ dọn các bảng phụ thuộc (wishlist, giỏ hàng, tồn kho...) trước khi xóa
            await _repository.DeleteAsync(Product);
            return true;
        }

        // [THÊM] Gắn sản phẩm vào bộ sưu tập / công nghệ (bảng Product_Collection - trước đây chưa có API)
        public async Task<(bool Success, string? Error)> AddToCollectionAsync(int productId, int collectionId)
        {
            if (await _repository.GetByIdAsync(productId) == null)
                return (false, "Sản phẩm không tồn tại.");
            if (!await _repository.CollectionExistsAsync(collectionId))
                return (false, "Bộ sưu tập / công nghệ không tồn tại.");
            if (await _repository.GetProductCollectionAsync(productId, collectionId) != null)
                return (false, "Sản phẩm đã thuộc bộ sưu tập / công nghệ này.");

            await _repository.AddProductCollectionAsync(new ProductCollection
            {
                ProductId = productId,
                CollectionId = collectionId
            });
            return (true, null);
        }

        // [THÊM] Gỡ sản phẩm khỏi bộ sưu tập / công nghệ
        public async Task<(bool Success, string? Error)> RemoveFromCollectionAsync(int productId, int collectionId)
        {
            var link = await _repository.GetProductCollectionAsync(productId, collectionId);
            if (link == null)
                return (false, "Sản phẩm không thuộc bộ sưu tập / công nghệ này.");

            await _repository.RemoveProductCollectionAsync(link);
            return (true, null);
        }
    }
}