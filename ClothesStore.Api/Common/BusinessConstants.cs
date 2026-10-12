namespace ClothesStore.Api.Common
{
    // Trạng thái đơn hàng (Order.Status)
    public static class OrderStatus
    {
        public const string Pending = "Pending";       // vừa đặt, chờ xác nhận (được sửa / hủy)
        public const string Confirmed = "Confirmed";   // nhân viên đã xác nhận, chờ giao / chờ khách tới lấy
        public const string Shipping = "Shipping";     // đã bàn giao cho đơn vị vận chuyển
        public const string Completed = "Completed";   // khách đã nhận hàng
        public const string Cancelled = "Cancelled";   // đã hủy (hoàn kho)
        public const string Returned = "Returned";     // toàn bộ sản phẩm đã được trả lại

        public static readonly string[] All = { Pending, Confirmed, Shipping, Completed, Cancelled, Returned };
    }

    // Trạng thái giao dịch thanh toán (PaymentTransaction.Status)
    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded";
        public const string PartiallyRefunded = "PartiallyRefunded";

        public static readonly string[] All = { Pending, Paid, Failed, Cancelled, Refunded, PartiallyRefunded };
    }

    // Phương thức thanh toán
    public static class PaymentMethods
    {
        public const string Cod = "COD";
        public static readonly string[] All = { "COD", "Momo", "VNPay", "BankTransfer", "Cash" };
    }

    // Trạng thái vận chuyển (Shipment.Status)
    public static class ShipmentStatus
    {
        public const string Shipping = "Shipping";
        public const string Delivered = "Delivered";
        public const string Failed = "Failed";

        public static readonly string[] All = { Shipping, Delivered, Failed };
    }

    // Trạng thái yêu cầu đổi/trả (ReturnRequest.Status)
    public static class ReturnStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Completed = "Completed";
    }

    // Quy tắc tích điểm
    public static class LoyaltyRules
    {
        // 10.000đ = 1 điểm
        public const decimal AmountPerPoint = 10000m;

        public static int CalculatePoints(decimal amount)
            => amount <= 0 ? 0 : (int)Math.Floor(amount / AmountPerPoint);
    }

    // Nhóm role dùng cho [Authorize(Roles = ...)]
    public static class RoleGroups
    {
        public const string Admin = "Admin";
        public const string Staff = "Admin,Employee";
        public const string Customer = "Customer";
    }
}
