namespace ClothesStore.Api.DTOs.StoreStock
{
    public class CreateStoreStockDto
    {
        public int StoreId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
