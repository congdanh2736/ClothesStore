using System.Runtime.CompilerServices;
using AutoMapper;
using ClothesStore.Api.DTOs.CollectionTech;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class CollectionTechService : ICollectionTechService{

        private readonly IMapper _mapper;
        private readonly ICollectionTechRepository _repository;

        public CollectionTechService(ICollectionTechRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        // Lấy toàn bộ danh sách bộ sư tập và công nghệ
        public async Task<IEnumerable<CollectionTechDto>> GetAllAsync()
        {
            var CollectionTech = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<CollectionTechDto>>(CollectionTech);
        }

        // Lấy chi tiết 1 bộ sư tập và công nghệ theo Id
        public async Task<CollectionTechDto?> GetByIdAsync(int id)
        {
            var CollectionTech = await _repository.GetByIdWithDetailsAsync(id);
            //Nếu người dùng tìm người với id không tồn tại thì trả về null. vd: id =999999999999 trả về null
            if (CollectionTech == null)
                return null;

            return _mapper.Map<CollectionTechDto>(CollectionTech);
        }

        // Tạo mới bộ sư tập và công nghệ
        public async Task<(bool Success, string? Error, CollectionTechDto? Data)> CreateAsync(CreateCollectionTechDto dto)
        {
            // Chuyển DTO sang Model để lưu vào database
            var CollectionTech = _mapper.Map<CollectionTech>(dto);
            await _repository.AddAsync(CollectionTech);
            // Trả về thành công kèm theo dữ liệu DTO vừa tạo
            return (true, null, _mapper.Map<CollectionTechDto>(CollectionTech));
        }

        //Chỉnh sửa bộ sư tập và công nghệ
        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateCollectionTechDto dto)
        {
            var CollectionTech = await _repository.GetByIdAsync(id);
            //kiểm tra danh mục có tồn tại hay không
            if (CollectionTech == null)
                return (false, "id của 'bộ sư tập và công nghệ' không tìm thấy");

            //Cập nhật thông tin 
            CollectionTech.Name = dto.Name;
            CollectionTech.Type = dto.Type;

            await _repository.UpdateAsync(CollectionTech);
            return (true,null);
        }

        // xóa bộ sư tập và công nghệ
        public async Task<bool> DeleteAsync(int id)
        {
            var CollectionTech = await _repository.GetByIdAsync(id);

            if (CollectionTech ==null) return false;
            await _repository.DeleteAsync(CollectionTech);
            return true;
        }
    }
}