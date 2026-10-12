using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] dùng BusinessException khi chặn xóa danh mục
using ClothesStore.Api.DTOs.Category;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CategoryService : ICategoryService{

        private readonly ICategoryRepository _repository;
        private readonly IMapper _mapper;

        //Tiêm phụ thuộc qua hàm khởi tạo
        public CategoryService(ICategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }


        // Lấy toàn bộ danh sách danh mục
        public async Task<IEnumerable<CategoryDTO>> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryDTO>>(categories);
        }

        // Lấy chi tiết 1 danh mục theo Id
        public async Task<CategoryDTO?> GetByIdAsync(int id)
        {
            var category = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if( category == null) 
                return null;
            return _mapper.Map<CategoryDTO>(category);
        }

        // Tạo mới danh mục
        public async Task<(bool Success, string? Error, CategoryDTO? Data)> CreateAsync(CreateCategoryDto dto)
        {
            //Kiểm tra danh mục tra cha có tồn tại hay không.
            if (dto.ParentCategoryId.HasValue)
            {
                var ExitParentCategory = await _repository.GetByIdAsync(dto.ParentCategoryId.Value);
                if (ExitParentCategory == null)
                    // [SỬA] rút gọn thông báo lỗi cho dễ hiểu với người dùng
                    return (false, "Danh mục cha không tồn tại.", null);
            }

            // Chuyển DTO sang Model để lưu vào database
            var category = _mapper.Map<Category>(dto);
            await _repository.AddAsync(category);

            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true,null, _mapper.Map<CategoryDTO>(category));
        }

        //Chỉnh sửa danh mục
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateCategoryDto dto)
        {

            var category = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (category == null)
                return (false, "id danh mục không tìm thấy.");

            // [THÊM] Kiểm tra danh mục cha khi cập nhật (trước đây không kiểm tra -> lỗi khóa ngoại 500,
            //        hoặc tạo vòng lặp cây danh mục: A là cha của B, B lại là cha của A)
            if (dto.ParentCategoryId.HasValue)
            {
                // [THÊM] Không cho danh mục tự làm cha của chính nó
                if (dto.ParentCategoryId.Value == id)
                    return (false, "Danh mục không thể là danh mục cha của chính nó.");

                // [THÊM] Đi ngược lên các cấp cha, nếu gặp lại chính danh mục này -> tạo vòng lặp
                var currentId = dto.ParentCategoryId;
                while (currentId.HasValue)
                {
                    var current = await _repository.GetByIdAsync(currentId.Value);
                    if (current == null)
                        return (false, "Danh mục cha không tồn tại.");
                    if (current.ParentCategoryId == id)
                        return (false, "Không thể chọn danh mục con làm danh mục cha (tạo vòng lặp).");
                    currentId = current.ParentCategoryId;
                }
            }

            //Cập nhật thông tin 
            category.Name = dto.Name;
            category.ParentCategoryId = dto.ParentCategoryId;

            await _repository.UpdateAsync(category);
            return (true,null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            // [SỬA] GetByIdAsync -> GetByIdWithDetailsAsync để lấy kèm danh mục con và sản phẩm
            var category = await _repository.GetByIdWithDetailsAsync(id);
            if (category == null) return false;

            // [THÊM] Chặn xóa danh mục đang có danh mục con / sản phẩm
            //        (khóa ngoại để Restrict nên trước đây xóa sẽ bị lỗi 500)
            if (category.ChildrenCategories != null && category.ChildrenCategories.Any())
                throw new BusinessException("Danh mục đang có danh mục con, hãy xóa hoặc chuyển danh mục con trước.");
            if (category.Products != null && category.Products.Any())
                throw new BusinessException("Danh mục đang có sản phẩm, không thể xóa.");

            await _repository.DeleteAsync(category);
            return true;
        }
    }
}