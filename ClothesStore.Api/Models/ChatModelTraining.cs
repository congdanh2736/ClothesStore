// [THÊM MỚI] Chatbot AI - lịch sử các lần huấn luyện lại mô hình từ trang admin
using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class ChatModelTraining
    {
        [Key]
        public int Id { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Running";       // Running | Succeeded | Failed

        public int BaseSamples { get; set; }                  // số câu trong file intents.csv
        public int AdminSamples { get; set; }                 // số câu admin thêm (bảng ChatTrainingSamples)

        public double? MicroAccuracy { get; set; }            // tỉ lệ câu đoán đúng trên 20% dữ liệu tách ra
        public double? MacroAccuracy { get; set; }            // trung bình độ chính xác các intent
        public double? HandTestAccuracy { get; set; }         // độ chính xác trên câu viết tay (test_viet_tay.csv)
        public int? HandTestCount { get; set; }

        [MaxLength(1000)]
        public string? Message { get; set; }                  // ghi chú / lỗi

        [MaxLength(256)]
        public string? TriggeredBy { get; set; }              // email người bấm train
    }
}
