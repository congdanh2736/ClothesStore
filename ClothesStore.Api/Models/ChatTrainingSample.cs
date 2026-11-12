// [THÊM MỚI] Chatbot AI - câu huấn luyện do admin thêm (bổ sung cho file intents.csv khi train lại)
using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class ChatTrainingSample
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;      // câu hỏi mẫu

        [MaxLength(50)]
        public string Label { get; set; } = string.Empty;     // intent đúng của câu

        [MaxLength(20)]
        public string Source { get; set; } = "Admin";         // "Admin" = tự nhập, "ChatLog" = lấy từ lịch sử chat

        public int? ChatLogId { get; set; }                   // lấy từ câu chat nào (nếu có)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
