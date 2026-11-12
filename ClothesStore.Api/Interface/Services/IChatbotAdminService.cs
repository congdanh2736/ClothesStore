// [THÊM MỚI] Chatbot AI - service cho trang quản lý chatbot
using ClothesStore.Api.DTOs.Chatbot;

namespace ClothesStore.Api.Interface.Services
{
    public interface IChatbotAdminService
    {
        Task<ChatStatsDto> GetStatsAsync(int days);
        Task<PagedResultDto<ChatLogDto>> GetLogsAsync(ChatLogQuery query);
        Task<ChatLogDto> ReviewLogAsync(int id, ReviewChatLogDto dto);
        Task DeleteLogAsync(int id);

        Task<List<ChatIntentInfoDto>> GetIntentsAsync();
        Task<List<ChatTrainingSampleDto>> GetSamplesAsync(string? label);
        Task<ChatTrainingSampleDto> CreateSampleAsync(SaveTrainingSampleDto dto);
        Task<ChatTrainingSampleDto> UpdateSampleAsync(int id, SaveTrainingSampleDto dto);
        Task DeleteSampleAsync(int id);

        Task<ChatModelInfoDto> GetModelInfoAsync();
        Task<ChatTrainingRunDto> StartTrainingAsync(string? triggeredBy);
        Task<ChatTrainingRunDto> GetTrainingAsync(int id);
    }
}
