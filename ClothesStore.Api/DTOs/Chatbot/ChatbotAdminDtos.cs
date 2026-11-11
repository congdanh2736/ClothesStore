// [THÊM MỚI] Chatbot AI - dữ liệu cho trang quản lý chatbot (admin)
namespace ClothesStore.Api.DTOs.Chatbot
{
    /* ----------------------------------------------------------------- Thống kê */
    public class ChatStatsDto
    {
        public int Days { get; set; }
        public int TotalMessages { get; set; }
        public int LoggedInMessages { get; set; }        // câu hỏi của khách đã đăng nhập
        public double AverageConfidence { get; set; }
        public int LowConfidenceCount { get; set; }      // mô hình chắc chắn dưới 50%
        public int NotUnderstoodCount { get; set; }      // bot trả lời "chưa hiểu"
        public int HelpfulCount { get; set; }            // 👍
        public int UnhelpfulCount { get; set; }          // 👎
        public int UnreviewedCount { get; set; }         // admin chưa xem
        public List<ChatIntentCountDto> ByIntent { get; set; } = new();
        public List<ChatDayCountDto> ByDay { get; set; } = new();
    }

    public class ChatIntentCountDto
    {
        public string Intent { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageConfidence { get; set; }
    }

    public class ChatDayCountDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    /* ----------------------------------------------------------------- Lịch sử hội thoại */
    public class ChatLogQuery
    {
        public string? Intent { get; set; }
        /// <summary>all | low (độ tin cậy thấp / chưa hiểu) | unhelpful (khách bấm 👎) | unreviewed (admin chưa xem)</summary>
        public string? Filter { get; set; }
        public string? Q { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ChatLogDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public string IntentLabel { get; set; } = string.Empty;
        public string? PredictedIntent { get; set; }
        public float Confidence { get; set; }
        public int ProductCount { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public bool? IsHelpful { get; set; }
        public bool IsReviewed { get; set; }
        public string? CorrectIntent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    /* Admin xem xong 1 câu: gán intent đúng (nếu bot hiểu sai) và đưa vào dữ liệu huấn luyện */
    public class ReviewChatLogDto
    {
        public string? CorrectIntent { get; set; }
        public bool AddToTraining { get; set; }
    }

    /* ----------------------------------------------------------------- Dữ liệu huấn luyện */
    public class ChatIntentInfoDto
    {
        public string Intent { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int BaseSamples { get; set; }     // số câu trong intents.csv
        public int AdminSamples { get; set; }    // số câu admin thêm
    }

    public class ChatTrainingSampleDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string LabelName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int? ChatLogId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveTrainingSampleDto
    {
        public string Text { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    /* ----------------------------------------------------------------- Mô hình + lần huấn luyện */
    public class ChatModelInfoDto
    {
        public bool ModelExists { get; set; }
        public long ModelSizeBytes { get; set; }
        public DateTime? ModelUpdatedAt { get; set; }
        public bool ModelLoaded { get; set; }            // server đã nạp mô hình lúc khởi động
        public bool BaseDataFound { get; set; }
        public int BaseSamples { get; set; }
        public int AdminSamples { get; set; }
        public bool HandTestFound { get; set; }
        public bool IsTraining { get; set; }
        public List<ChatTrainingRunDto> Runs { get; set; } = new();
    }

    public class ChatTrainingRunDto
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int BaseSamples { get; set; }
        public int AdminSamples { get; set; }
        public double? MicroAccuracy { get; set; }
        public double? MacroAccuracy { get; set; }
        public double? HandTestAccuracy { get; set; }
        public int? HandTestCount { get; set; }
        public string? Message { get; set; }
        public string? TriggeredBy { get; set; }
    }
}
