// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;

namespace ClothesStore.Api.DTOs.Order
{
    // [THÊM] Chỉ được sửa khi đơn còn ở trạng thái Pending
    public class UpdateOrderRequest
    {
        public int AddressId { get; set; }
        // [SỬA] PromotionId không còn bắt buộc (int -> int?) (trước đây: public int PromotionId { get; set; })
        public int? PromotionId { get; set; }
        public List<UpdateOrderItemRequest>? Items { get; set; }
    }
}
