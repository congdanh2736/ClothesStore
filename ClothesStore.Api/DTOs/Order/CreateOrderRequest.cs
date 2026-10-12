// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;
// [XÓA] using không còn dùng: ClothesStore.Api.Models;

namespace ClothesStore.Api.DTOs.Order
{
    public class CreateOrderRequest
    {
        public int CustomerId { get; set; }
        public int AddressId { get; set; }
        // [SỬA] PromotionId không còn bắt buộc (int -> int?) (trước đây: public int PromotionId { get; set; })
        public int? PromotionId { get; set; }      // không bắt buộc
        public int? StoreId { get; set; }          // cửa hàng xuất kho, null = hệ thống tự chọn cửa hàng còn đủ hàng
        public string? PaymentMethod { get; set; }
        public List<CreateOrderItemRequest>? Items { get; set; }
    }
}
