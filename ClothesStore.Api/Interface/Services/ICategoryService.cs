using ClothesStore.Api.DTOs.Category;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface ICategoryService : IService<CategoryDTO,CreateCategoryDto,UpdateCategoryDto> {
        
    }
}