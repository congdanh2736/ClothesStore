// [THÊM MỚI] Chatbot AI - lịch sử hội thoại (mỗi dòng = 1 câu hỏi của khách + câu trả lời của bot)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class ChatLog
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;   // câu khách hỏi

        [MaxLength(2000)]
        public string Reply { get; set; } = string.Empty;     // câu bot trả lời

        [MaxLength(50)]
        public string Intent { get; set; } = string.Empty;    // intent bot dùng để trả lời

        [MaxLength(50)]
        public string? PredictedIntent { get; set; }          // intent mô hình đoán (có thể khác Intent khi mô hình không chắc chắn)

        public float Confidence { get; set; }                 // độ tin cậy của mô hình (0 - 1)
        public int ProductCount { get; set; }                 // số sản phẩm bot gợi ý

        // Khách đã đăng nhập thì lưu lại (khách vãng lai = null). Xóa khách -> giữ lịch sử, CustomerId thành null.
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public bool? IsHelpful { get; set; }                  // khách bấm 👍 (true) / 👎 (false), chưa bấm = null

        // Phần dành cho admin
        public bool IsReviewed { get; set; }                  // admin đã xem
        [MaxLength(50)]
        public string? CorrectIntent { get; set; }            // intent đúng do admin gán (để train lại)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
