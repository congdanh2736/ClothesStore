// [THÊM MỚI] Chatbot AI - nghiệp vụ trang quản lý chatbot: thống kê, xem lịch sử, gán nhãn, câu huấn luyện, huấn luyện lại
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using ClothesStore.Api.Services.Chatbot;

namespace ClothesStore.Api.Services
{
    public class ChatbotAdminService : IChatbotAdminService
    {
        /// <summary>Độ tin cậy dưới mức này = "bot không chắc chắn" (giống ChatbotService)</summary>
        private const float LowConfidence = 0.5f;

        private readonly IChatbotAdminRepository _repository;
        private readonly IChatbotTrainingRunner _runner;

        public ChatbotAdminService(IChatbotAdminRepository repository, IChatbotTrainingRunner runner)
        {
            _repository = repository;
            _runner = runner;
        }

        // ================================================================== Thống kê + lịch sử
        public async Task<ChatStatsDto> GetStatsAsync(int days)
        {
            days = Math.Clamp(days, 1, 365);
            var stats = await _repository.GetStatsAsync(DateTime.UtcNow.Date.AddDays(1 - days), LowConfidence);
            stats.Days = days;
            return stats;
        }

        public Task<PagedResultDto<ChatLogDto>> GetLogsAsync(ChatLogQuery query) => _repository.GetLogsAsync(query, LowConfidence);

        /*
         * Admin xem 1 câu:
         *  - Đánh dấu đã xem.
         *  - Bot hiểu sai -> chọn intent đúng; tick "đưa vào dữ liệu huấn luyện" -> câu hỏi được thêm
         *    vào bảng ChatTrainingSamples, lần huấn luyện lại sau mô hình sẽ học câu này.
         */
        public async Task<ChatLogDto> ReviewLogAsync(int id, ReviewChatLogDto dto)
        {
            var log = await _repository.GetLogAsync(id) ?? throw new NotFoundException("Không tìm thấy câu hỏi.");
            if (dto.CorrectIntent != null && !ChatIntents.IsValid(dto.CorrectIntent))
                throw new BusinessException("Intent không hợp lệ.");

            log.IsReviewed = true;
            log.CorrectIntent = dto.CorrectIntent ?? log.CorrectIntent;
            await _repository.UpdateLogAsync(log);

            if (dto.AddToTraining)
            {
                var label = log.CorrectIntent ?? (ChatIntents.IsValid(log.Intent) ? log.Intent : null)
                    ?? throw new BusinessException("Hãy chọn intent đúng trước khi đưa vào dữ liệu huấn luyện.");
                var text = log.Message.Trim();
                if (!await _repository.SampleExistsAsync(text, label))
                    await _repository.AddSampleAsync(new ChatTrainingSample { Text = text, Label = label, Source = "ChatLog", ChatLogId = log.Id });
            }

            return new ChatLogDto
            {
                Id = log.Id, Message = log.Message, Reply = log.Reply, Intent = log.Intent, IntentLabel = LabelOf(log.Intent),
                PredictedIntent = log.PredictedIntent, Confidence = log.Confidence, ProductCount = log.ProductCount,
                CustomerId = log.CustomerId, IsHelpful = log.IsHelpful, IsReviewed = log.IsReviewed,
                CorrectIntent = log.CorrectIntent, CreatedAt = log.CreatedAt
            };
        }

        public async Task DeleteLogAsync(int id)
        {
            if (!await _repository.DeleteLogAsync(id)) throw new NotFoundException("Không tìm thấy câu hỏi.");
        }

        // ================================================================== Dữ liệu huấn luyện
        public async Task<List<ChatIntentInfoDto>> GetIntentsAsync()
        {
            var baseCounts = _runner.GetBaseCounts();
            var adminCounts = await _repository.CountSamplesByLabelAsync();
            return ChatIntents.All.Select(intent => new ChatIntentInfoDto
            {
                Intent = intent,
                Label = LabelOf(intent),
                BaseSamples = baseCounts.TryGetValue(intent, out var b) ? b : 0,
                AdminSamples = adminCounts.TryGetValue(intent, out var a) ? a : 0
            }).ToList();
        }

        public async Task<List<ChatTrainingSampleDto>> GetSamplesAsync(string? label)
            => (await _repository.GetSamplesAsync(label)).Select(ToDto).ToList();

        public async Task<ChatTrainingSampleDto> CreateSampleAsync(SaveTrainingSampleDto dto)
        {
            var (text, label) = Validate(dto);
            if (await _repository.SampleExistsAsync(text, label)) throw new BusinessException("Câu này đã có trong dữ liệu huấn luyện.");
            var sample = new ChatTrainingSample { Text = text, Label = label, Source = "Admin" };
            await _repository.AddSampleAsync(sample);
            return ToDto(sample);
        }

        public async Task<ChatTrainingSampleDto> UpdateSampleAsync(int id, SaveTrainingSampleDto dto)
        {
            var sample = await _repository.GetSampleAsync(id) ?? throw new NotFoundException("Không tìm thấy câu huấn luyện.");
            var (text, label) = Validate(dto);
            if (await _repository.SampleExistsAsync(text, label, id)) throw new BusinessException("Câu này đã có trong dữ liệu huấn luyện.");
            sample.Text = text;
            sample.Label = label;
            await _repository.UpdateSampleAsync(sample);
            return ToDto(sample);
        }

        public async Task DeleteSampleAsync(int id)
        {
            var sample = await _repository.GetSampleAsync(id) ?? throw new NotFoundException("Không tìm thấy câu huấn luyện.");
            await _repository.DeleteSampleAsync(sample);
        }

        // ================================================================== Mô hình + huấn luyện lại
        public async Task<ChatModelInfoDto> GetModelInfoAsync()
        {
            var file = new FileInfo(_runner.ModelPath);
            var adminCounts = await _repository.CountSamplesByLabelAsync();
            return new ChatModelInfoDto
            {
                ModelExists = file.Exists,
                ModelSizeBytes = file.Exists ? file.Length : 0,
                ModelUpdatedAt = file.Exists ? file.LastWriteTimeUtc : null,
                ModelLoaded = _runner.ModelLoaded,
                BaseDataFound = _runner.BaseDataPath != null,
                BaseSamples = _runner.GetBaseCounts().Values.Sum(),
                AdminSamples = adminCounts.Values.Sum(),
                HandTestFound = _runner.HandTestPath != null,
                IsTraining = _runner.IsTraining,
                Runs = (await _repository.GetTrainingsAsync(10)).Select(ToDto).ToList()
            };
        }

        public async Task<ChatTrainingRunDto> StartTrainingAsync(string? triggeredBy) => ToDto(await _runner.StartAsync(triggeredBy));

        public async Task<ChatTrainingRunDto> GetTrainingAsync(int id)
            => ToDto(await _repository.GetTrainingAsync(id) ?? throw new NotFoundException("Không tìm thấy lần huấn luyện."));

        // ================================================================== Hàm phụ
        private static (string Text, string Label) Validate(SaveTrainingSampleDto dto)
        {
            var text = (dto.Text ?? "").Trim();
            if (text.Length == 0) throw new BusinessException("Câu huấn luyện không được để trống.");
            if (text.Length > 500) throw new BusinessException("Câu huấn luyện tối đa 500 ký tự.");
            if (!ChatIntents.IsValid(dto.Label)) throw new BusinessException("Intent không hợp lệ.");
            return (text, dto.Label);
        }

        private static string LabelOf(string intent) => ChatIntents.Labels.TryGetValue(intent, out var label) ? label : "Chưa hiểu";

        private static ChatTrainingSampleDto ToDto(ChatTrainingSample s) => new()
        {
            Id = s.Id, Text = s.Text, Label = s.Label, LabelName = LabelOf(s.Label), Source = s.Source, ChatLogId = s.ChatLogId, CreatedAt = s.CreatedAt
        };

        private ChatTrainingRunDto ToDto(ChatModelTraining r) => new()
        {
            Id = r.Id, StartedAt = r.StartedAt, FinishedAt = r.FinishedAt,
            // Đang "Running" mà server không còn chạy (server đã tắt giữa chừng) -> coi như thất bại
            Status = r.Status == "Running" && !_runner.IsTraining ? "Interrupted" : r.Status,
            BaseSamples = r.BaseSamples, AdminSamples = r.AdminSamples,
            MicroAccuracy = r.MicroAccuracy, MacroAccuracy = r.MacroAccuracy,
            HandTestAccuracy = r.HandTestAccuracy, HandTestCount = r.HandTestCount,
            Message = r.Status == "Running" && !_runner.IsTraining ? "Server đã dừng giữa lúc huấn luyện." : r.Message,
            TriggeredBy = r.TriggeredBy
        };
    }
}
