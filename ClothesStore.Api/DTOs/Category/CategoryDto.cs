namespace ClothesStore.Api.DTOs.Category
{
    public class CategoryDTO 
    {
        public int Id {get; set;}
        public string? Name { get; set; } = string.Empty;
        public string? ParentCategoryName { get; set; } = string.Empty;
        public int ChildrenCategoriesCount { get; set; }
        public int ProductCount { get; set; }
    }
}
