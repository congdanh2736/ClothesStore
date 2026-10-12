using ClothesStore.Api.Models;

namespace ClothesStore.Api.Common
{
    // Tính lại tiền của đơn: Subtotal - giảm giá khuyến mãi = TotalAmount
    public static class OrderPricing
    {
        // Yêu cầu: order.OrderItems và order.Promotion (nếu có) đã được load
        public static void Recalculate(Order order)
        {
            var subtotal = order.OrderItems.Sum(item => item.Price * item.Quantity);
            var discount = order.Promotion is null ? 0m : Math.Min(order.Promotion.DiscountValue, subtotal);

            order.DiscountAmount = discount;
            order.TotalAmount = subtotal - discount;
        }

        public static bool IsPromotionActive(Promotion promotion, DateTime now)
            => promotion.StartDate <= now && promotion.EndDate >= now;
    }
}
