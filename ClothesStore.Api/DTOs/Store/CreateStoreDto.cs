namespace ClothesStore.Api.DTOs.Store
{
    public class CreateStoreDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
    }
}
