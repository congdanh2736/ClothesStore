// [THÊM MỚI] Chatbot AI - các truy vấn database bot cần (chỉ ĐỌC, không sửa dữ liệu)
using ClothesStore.Api.DTOs.Chatbot;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IChatbotRepository
    {
        /// <summary>Từ điển của shop: danh mục, màu, bộ sưu tập / công nghệ, cửa hàng, mã khuyến mãi, hạng thành viên</summary>
        Task<ChatVocabulary> GetVocabularyAsync();

        /// <summary>Tìm sản phẩm theo điều kiện, trả về (danh sách tối đa query.Take sản phẩm, tổng số sản phẩm khớp)</summary>
        Task<(List<ChatProductDto> Items, int Total)> SearchProductsAsync(ChatProductQuery query);

        /// <summary>Tên + danh mục của 1 sản phẩm (dùng khi khách đang xem sản phẩm đó)</summary>
        Task<ChatProductDto?> GetProductAsync(int productId);

        /// <summary>Tồn kho (số lượng > 0) của 1 sản phẩm theo từng cửa hàng, lọc theo màu / size / cửa hàng</summary>
        Task<List<ChatStockRow>> GetProductStockAsync(int productId, List<string> colors, List<string> sizes, List<int> storeIds);

        /// <summary>Bảng size của 1 danh mục</summary>
        Task<List<ChatSizeRow>> GetSizeChartAsync(int categoryId);

        /// <summary>Id khách hàng ứng với tài khoản đăng nhập (ApplicationUser.Id lấy từ token)</summary>
        Task<int?> GetCustomerIdByUserIdAsync(string applicationUserId);

        /// <summary>Đơn hàng gần nhất của khách (hoặc đúng 1 đơn nếu có orderId)</summary>
        Task<List<ChatOrderInfo>> GetOrdersAsync(int customerId, int? orderId, int take);
    }
}
