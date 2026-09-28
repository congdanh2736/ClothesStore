using AutoMapper;
using ClothesStore.Api.DTOs.Category;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class CategoryProfile : Profile 
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDTO>()
                .ForMember(c => c.ParentCategoryName,
                    a => a.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(c => c.ProductCount,
                    c => c.MapFrom(src => src.Products!.Count))
                .ForMember(c => c.ChildrenCategoriesCount,
                    a => a.MapFrom(src => src.ChildrenCategories!.Count));

            CreateMap<CreateCategoryDto,Category>();
        }
    }
}