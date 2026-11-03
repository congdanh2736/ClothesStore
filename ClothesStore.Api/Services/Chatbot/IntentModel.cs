// [THÊM MỚI] Chatbot AI - kiểu dữ liệu dùng chung với mô hình đã train (MLModels/intent-model.zip)
using Microsoft.ML.Data;

namespace ClothesStore.Api.Services.Chatbot
{
    /*
     * Đầu vào của mô hình. PHẢI giống hệt lớp IntentInput trong project ClothesStore.ChatbotTrainner
     * (mô hình nhận dữ liệu theo TÊN cột: "Text", "Label").
     */
    public class IntentInput
    {
        [LoadColumn(0)] public string Text { get; set; } = "";
        [LoadColumn(1)] public string Label { get; set; } = "";
    }

    /* Kết quả mô hình trả về */
    public class IntentPrediction
    {
        [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; } = "";

        // Điểm (xác suất) của từng intent, cộng lại = 1. Điểm cao nhất = độ tin cậy của câu trả lời.
        public float[] Score { get; set; } = [];
    }
}
