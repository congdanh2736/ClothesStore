namespace ClothesStore.Api.DTOs.StoreStock
{
    public class StoreStockDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
