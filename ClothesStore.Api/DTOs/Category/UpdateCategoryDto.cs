namespace ClothesStore.Api.DTOs.Category
{
    public class UpdateCategoryDto 
    {
        public string? Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
    }
}