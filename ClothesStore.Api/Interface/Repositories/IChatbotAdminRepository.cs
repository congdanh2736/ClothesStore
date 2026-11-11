// [THÊM MỚI] Chatbot AI - truy vấn cho trang quản lý chatbot (lịch sử chat, câu huấn luyện, lần huấn luyện)
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IChatbotAdminRepository
    {
        // ---- Lịch sử hội thoại ----
        Task<ChatStatsDto> GetStatsAsync(DateTime fromUtc, float lowConfidence);
        Task<PagedResultDto<ChatLogDto>> GetLogsAsync(ChatLogQuery query, float lowConfidence);
        Task<ChatLog?> GetLogAsync(int id);
        Task UpdateLogAsync(ChatLog log);
        Task<bool> DeleteLogAsync(int id);

        // ---- Câu huấn luyện do admin thêm ----
        Task<List<ChatTrainingSample>> GetSamplesAsync(string? label);
        Task<ChatTrainingSample?> GetSampleAsync(int id);
        Task<bool> SampleExistsAsync(string text, string label, int? excludeId = null);
        Task<Dictionary<string, int>> CountSamplesByLabelAsync();
        Task AddSampleAsync(ChatTrainingSample sample);
        Task UpdateSampleAsync(ChatTrainingSample sample);
        Task DeleteSampleAsync(ChatTrainingSample sample);

        // ---- Các lần huấn luyện ----
        Task<List<ChatModelTraining>> GetTrainingsAsync(int take);
        Task<ChatModelTraining?> GetTrainingAsync(int id);
        Task AddTrainingAsync(ChatModelTraining training);
        Task UpdateTrainingAsync(ChatModelTraining training);
    }
}
