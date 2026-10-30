// [THÊM MỚI] Chatbot AI - dữ liệu gửi lên / trả về của POST /api/Chatbot/ask
namespace ClothesStore.Api.DTOs.Chatbot
{
    /* Khách gửi 1 câu hỏi */
    public class ChatRequestDto
    {
        /// <summary>Câu hỏi của khách, ví dụ "cao 1m70 nặng 65kg mặc áo thun size gì"</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Ngữ cảnh của câu trước (lấy từ phản hồi lần trước gửi lại) để bot hiểu câu hỏi nối tiếp</summary>
        public ChatContextDto? Context { get; set; }
    }

    /*
     * Ngữ cảnh hội thoại: bot không lưu gì trên server, giao diện tự giữ và gửi lại mỗi lần hỏi.
     * Ví dụ hỏi "tìm áo khoác đen" rồi hỏi tiếp "còn size L không" -> bot vẫn biết đang nói về áo khoác.
     */
    public class ChatContextDto
    {
        public string? LastIntent { get; set; }
        public int? CategoryId { get; set; }
        public List<string> Colors { get; set; } = new();

        /// <summary>Sản phẩm đang xem (giao diện gửi lên khi khách mở chat ở trang chi tiết sản phẩm)</summary>
        public int? ProductId { get; set; }
    }

    /* Bot trả lời */
    public class ChatResponseDto
    {
        /// <summary>Câu trả lời (xuống dòng bằng \n, gạch đầu dòng bằng "• ")</summary>
        public string Reply { get; set; } = string.Empty;

        /// <summary>Intent mô hình nhận diện được (để hiển thị / ghi log / báo cáo)</summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>Độ tin cậy của mô hình (0 - 1)</summary>
        public float Confidence { get; set; }

        /// <summary>Sản phẩm gợi ý (giao diện vẽ thành thẻ sản phẩm)</summary>
        public List<ChatProductDto> Products { get; set; } = new();

        /// <summary>Đường dẫn liên quan (xem bảng size, đơn hàng, cửa hàng...)</summary>
        public List<ChatLinkDto> Links { get; set; } = new();

        /// <summary>Câu hỏi gợi ý để khách bấm nhanh</summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>Ngữ cảnh mới - giao diện gửi lại ở câu hỏi sau</summary>
        public ChatContextDto Context { get; set; } = new();
    }

    public class ChatProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal MinPrice { get; set; }
        public List<string> Colors { get; set; } = new();
        public bool InStock { get; set; }
    }

    public class ChatLinkDto
    {
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
