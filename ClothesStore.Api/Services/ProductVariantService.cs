using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException khi chặn xóa biến thể
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

        // [THÊM] Danh sách biến thể (màu/size/giá) của 1 sản phẩm
        public async Task<IEnumerable<ProductVariantDto>> GetByProductAsync(int productId)
        {
            var variants = await _repository.GetByProductIdAsync(productId);
            return _mapper.Map<IEnumerable<ProductVariantDto>>(variants);
        }

        // Tạo mới Phiên bản
        public async Task<(bool Success, string? Error, ProductVariantDto? Data)> CreateAsync(CreateProductVariantDto dto)
        {
            // [THÊM] Sản phẩm phải tồn tại (trước đây ProductId sai -> lỗi khóa ngoại 500)
            if (!await _repository.ProductExistsAsync(dto.ProductId))
                return (false, "Sản phẩm không tồn tại.", null);
            // [THÊM] Không cho tạo 2 biến thể trùng (cùng sản phẩm, màu, size)
            if (await _repository.DuplicateExistsAsync(dto.ProductId, dto.Color, dto.Size))
                return (false, "Sản phẩm đã có biến thể cùng màu và kích cỡ.", null);

            // Chuyển DTO sang Model để lưu vào database
            var ProductVariant = _mapper.Map<ProductVariant>(dto);
            await _repository.AddAsync(ProductVariant);

            // [SỬA] Load lại kèm Product để DTO trả về có ProductName
            var created = await _repository.GetByIdWithDetailsAsync(ProductVariant.VariantId);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<ProductVariantDto>(created));
        }

        //Chỉnh sửa Phiên bản
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductVariantDto dto)
        {
            var ProductVariant = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (ProductVariant == null)
                return (false, "id của 'biến thể sản phẩm' không tìm thấy");

            // [THÊM] Không cho sửa thành màu + size trùng với biến thể khác của cùng sản phẩm
            if (await _repository.DuplicateExistsAsync(ProductVariant.ProductId, dto.Color, dto.Size, id))
                return (false, "Sản phẩm đã có biến thể cùng màu và kích cỡ.");

            //Cập nhật thông tin 
            // (Đổi giá không ảnh hưởng đơn cũ vì OrderItem đã lưu giá tại thời điểm đặt)
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

            // [THÊM] Biến thể đã có trong đơn hàng thì không được xóa
            if (await _repository.HasOrdersAsync(id))
                throw new BusinessException("Biến thể đã phát sinh đơn hàng, không thể xóa.");

            // [SỬA] Repository.DeleteAsync giờ dọn giỏ hàng / tồn kho / thống kê của biến thể trước khi xóa
            await _repository.DeleteAsync(ProductVariant);
            return true;
        }
    }
}