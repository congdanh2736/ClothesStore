using AutoMapper;
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
                    return (false, "Danh mục cha có giá trị (hasValue) nhưng không tồn tại (không tìm thấy Id)", null);
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

            //Cập nhật thông tin 
            category.Name = dto.Name;
            category.ParentCategoryId = dto.ParentCategoryId;

            await _repository.UpdateAsync(category);
            return (true,null);
        }

        // xóa danh mục
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return false;

            await _repository.DeleteAsync(category);
            return true;
        }
    }
}