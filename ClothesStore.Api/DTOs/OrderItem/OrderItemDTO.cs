// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
namespace ClothesStore.Api.DTOs.OrderItem
{
    public class OrderItemDTO
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }          // [THÊM]
        public int VariantId { get; set ; }
        public int ProductId { get; set; }        // [THÊM]
        public string? VariantName { get; set; }   // tên sản phẩm
        public string? Color { get; set; }        // [THÊM]
        public string? Size { get; set; }         // [THÊM]
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal => Price * Quantity; // [THÊM] thành tiền của dòng
    }
}
