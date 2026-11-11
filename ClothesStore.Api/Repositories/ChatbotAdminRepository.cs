// [THÊM MỚI] Chatbot AI - truy vấn cho trang quản lý chatbot
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using ClothesStore.Api.Services.Chatbot;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class ChatbotAdminRepository : IChatbotAdminRepository
    {
        private readonly ApplicationDbContext _context;
        public ChatbotAdminRepository(ApplicationDbContext context) => _context = context;

        // ================================================================== Thống kê
        public async Task<ChatStatsDto> GetStatsAsync(DateTime fromUtc, float lowConfidence)
        {
            var logs = _context.ChatLogs.AsNoTracking().Where(x => x.CreatedAt >= fromUtc);

            var byIntent = await logs.GroupBy(x => x.Intent)
                .Select(g => new { Intent = g.Key, Count = g.Count(), Avg = g.Average(x => (double)x.Confidence) })
                .ToListAsync();
            var byDay = await logs.GroupBy(x => x.CreatedAt.Date)
                .Select(g => new ChatDayCountDto { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var total = byIntent.Sum(x => x.Count);
            return new ChatStatsDto
            {
                TotalMessages = total,
                LoggedInMessages = await logs.CountAsync(x => x.CustomerId != null),
                AverageConfidence = total == 0 ? 0 : byIntent.Sum(x => x.Avg * x.Count) / total,
                LowConfidenceCount = await logs.CountAsync(x => x.Confidence < lowConfidence),
                NotUnderstoodCount = await logs.CountAsync(x => x.Intent == ChatIntents.KhongHieu),
                HelpfulCount = await logs.CountAsync(x => x.IsHelpful == true),
                UnhelpfulCount = await logs.CountAsync(x => x.IsHelpful == false),
                UnreviewedCount = await logs.CountAsync(x => !x.IsReviewed && (x.Confidence < lowConfidence || x.IsHelpful == false || x.Intent == ChatIntents.KhongHieu)),
                ByIntent = byIntent.OrderByDescending(x => x.Count).Select(x => new ChatIntentCountDto
                {
                    Intent = x.Intent,
                    Label = ChatIntents.Labels.TryGetValue(x.Intent, out var label) ? label : "Chưa hiểu",
                    Count = x.Count,
                    AverageConfidence = x.Avg
                }).ToList(),
                ByDay = byDay
            };
        }

        // ================================================================== Lịch sử hội thoại
        public async Task<PagedResultDto<ChatLogDto>> GetLogsAsync(ChatLogQuery q, float lowConfidence)
        {
            var logs = _context.ChatLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q.Intent)) logs = logs.Where(x => x.Intent == q.Intent);
            logs = q.Filter switch
            {
                "low" => logs.Where(x => x.Confidence < lowConfidence || x.Intent == ChatIntents.KhongHieu),
                "unhelpful" => logs.Where(x => x.IsHelpful == false),
                "unreviewed" => logs.Where(x => !x.IsReviewed && (x.Confidence < lowConfidence || x.IsHelpful == false || x.Intent == ChatIntents.KhongHieu)),
                _ => logs
            };
            if (!string.IsNullOrWhiteSpace(q.Q)) logs = logs.Where(x => x.Message.Contains(q.Q));

            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 5, 100);
            var total = await logs.CountAsync();
            var items = await logs.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                .Select(x => new ChatLogDto
                {
                    Id = x.Id,
                    Message = x.Message,
                    Reply = x.Reply,
                    Intent = x.Intent,
                    PredictedIntent = x.PredictedIntent,
                    Confidence = x.Confidence,
                    ProductCount = x.ProductCount,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer != null ? x.Customer.FirstName + " " + x.Customer.LastName : null,
                    IsHelpful = x.IsHelpful,
                    IsReviewed = x.IsReviewed,
                    CorrectIntent = x.CorrectIntent,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
            foreach (var item in items)
                item.IntentLabel = ChatIntents.Labels.TryGetValue(item.Intent, out var label) ? label : "Chưa hiểu";

            return new PagedResultDto<ChatLogDto> { Items = items, Total = total, Page = page, PageSize = size };
        }

        public Task<ChatLog?> GetLogAsync(int id) => _context.ChatLogs.FirstOrDefaultAsync(x => x.Id == id);

        public async Task UpdateLogAsync(ChatLog log)
        {
            _context.ChatLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteLogAsync(int id)
        {
            var log = await _context.ChatLogs.FirstOrDefaultAsync(x => x.Id == id);
            if (log == null) return false;
            _context.ChatLogs.Remove(log);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================================================================== Câu huấn luyện
        public Task<List<ChatTrainingSample>> GetSamplesAsync(string? label)
            => _context.ChatTrainingSamples.AsNoTracking()
                .Where(x => label == null || label == "" || x.Label == label)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

        public Task<ChatTrainingSample?> GetSampleAsync(int id) => _context.ChatTrainingSamples.FirstOrDefaultAsync(x => x.Id == id);

        public Task<bool> SampleExistsAsync(string text, string label, int? excludeId = null)
            => _context.ChatTrainingSamples.AnyAsync(x => x.Text == text && x.Label == label && (excludeId == null || x.Id != excludeId));

        public Task<Dictionary<string, int>> CountSamplesByLabelAsync()
            => _context.ChatTrainingSamples.AsNoTracking()
                .GroupBy(x => x.Label)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

        public async Task AddSampleAsync(ChatTrainingSample sample)
        {
            _context.ChatTrainingSamples.Add(sample);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSampleAsync(ChatTrainingSample sample)
        {
            _context.ChatTrainingSamples.Update(sample);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSampleAsync(ChatTrainingSample sample)
        {
            _context.ChatTrainingSamples.Remove(sample);
            await _context.SaveChangesAsync();
        }

        // ================================================================== Các lần huấn luyện
        public Task<List<ChatModelTraining>> GetTrainingsAsync(int take)
            => _context.ChatModelTrainings.AsNoTracking().OrderByDescending(x => x.Id).Take(take).ToListAsync();

        public Task<ChatModelTraining?> GetTrainingAsync(int id) => _context.ChatModelTrainings.FirstOrDefaultAsync(x => x.Id == id);

        public async Task AddTrainingAsync(ChatModelTraining training)
        {
            _context.ChatModelTrainings.Add(training);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTrainingAsync(ChatModelTraining training)
        {
            _context.ChatModelTrainings.Update(training);
            await _context.SaveChangesAsync();
        }
    }
}
