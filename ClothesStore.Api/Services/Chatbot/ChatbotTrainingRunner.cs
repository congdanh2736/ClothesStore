// [THÊM MỚI] Chatbot AI - chạy huấn luyện lại ở chế độ nền (không bắt trình duyệt phải chờ vài phút)
using ClothesStore.Api.Common;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.Extensions.ML;

namespace ClothesStore.Api.Services.Chatbot
{
    public interface IChatbotTrainingRunner
    {
        bool IsTraining { get; }
        bool ModelLoaded { get; }                       // server đã nạp mô hình lúc khởi động
        string ModelPath { get; }
        string? BaseDataPath { get; }                   // file intents.csv (null = không tìm thấy)
        string? HandTestPath { get; }                   // file test_viet_tay.csv (null = không có)
        IReadOnlyDictionary<string, int> GetBaseCounts(); // số câu mỗi intent trong intents.csv
        Task<ChatModelTraining> StartAsync(string? triggeredBy);
    }

    /*
     * Đăng ký SINGLETON (1 đối tượng cho cả server) để biết đang có lần huấn luyện nào chạy không.
     * Mỗi lúc chỉ cho chạy 1 lần huấn luyện (SemaphoreSlim). Huấn luyện chạy trên luồng nền (Task.Run),
     * API trả về ngay Id lần huấn luyện; trang admin hỏi lại trạng thái vài giây 1 lần.
     */
    public class ChatbotTrainingRunner : IChatbotTrainingRunner
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _services;
        private readonly ILogger<ChatbotTrainingRunner> _logger;
        private IReadOnlyDictionary<string, int>? _baseCounts;
        private volatile bool _isTraining;

        public ChatbotTrainingRunner(IServiceScopeFactory scopeFactory, IServiceProvider services, IWebHostEnvironment env,
            IConfiguration config, ILogger<ChatbotTrainingRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _services = services;
            _logger = logger;

            var root = env.ContentRootPath;
            ModelPath = Path.Combine(root, "MLModels", "intent-model.zip");
            /*
             * Tìm file dữ liệu huấn luyện. Mặc định lấy ở project Trainer (cùng solution);
             * khi triển khai lên server thì chép intents.csv vào MLModels/ hoặc khai báo
             * "Chatbot": { "BaseDataPath": "...", "HandTestPath": "..." } trong appsettings.json.
             */
            BaseDataPath = FindFile(root, config["Chatbot:BaseDataPath"], "MLModels/intents.csv",
                "../ClothesStore.ChatbotTrainner/Data/intents.csv", "../ClothesStore.ChatbotTrainer/Data/intents.csv");
            HandTestPath = FindFile(root, config["Chatbot:HandTestPath"], "MLModels/test_viet_tay.csv",
                "../ClothesStore.ChatbotTrainner/Data/test_viet_tay.csv", "../ClothesStore.ChatbotTrainer/Data/test_viet_tay.csv");
        }

        public bool IsTraining => _isTraining;
        public string ModelPath { get; }
        public string? BaseDataPath { get; }
        public string? HandTestPath { get; }

        public bool ModelLoaded
        {
            get
            {
                try { return _services.GetService<PredictionEnginePool<IntentInput, IntentPrediction>>() != null; }
                catch { return false; }
            }
        }

        public IReadOnlyDictionary<string, int> GetBaseCounts()
        {
            if (_baseCounts != null) return _baseCounts;
            if (BaseDataPath == null) return new Dictionary<string, int>();
            _baseCounts = IntentModelTrainer.ReadCsv(BaseDataPath).GroupBy(x => x.Label).ToDictionary(g => g.Key, g => g.Count());
            return _baseCounts;
        }

        public async Task<ChatModelTraining> StartAsync(string? triggeredBy)
        {
            if (BaseDataPath == null)
                throw new BusinessException("Không tìm thấy file dữ liệu huấn luyện intents.csv (xem ChatbotTrainingRunner để biết các vị trí được tìm).");
            if (!Gate.Wait(0))
                throw new BusinessException("Đang có 1 lần huấn luyện chạy, vui lòng chờ xong.", StatusCodes.Status409Conflict);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IChatbotAdminRepository>();
                var samples = await repository.GetSamplesAsync(null);
                var run = new ChatModelTraining
                {
                    StartedAt = DateTime.UtcNow,
                    Status = "Running",
                    BaseSamples = GetBaseCounts().Values.Sum(),
                    AdminSamples = samples.Count,
                    TriggeredBy = triggeredBy
                };
                await repository.AddTrainingAsync(run);

                var adminRows = samples.Select(s => new IntentInput { Text = TextNormalizer.ForModel(s.Text), Label = s.Label }).ToList();
                _isTraining = true;
                _ = Task.Run(() => RunAsync(run.Id, adminRows)); // chạy nền, không chờ
                return run;
            }
            catch
            {
                Gate.Release();
                throw;
            }
        }

        private async Task RunAsync(int runId, List<IntentInput> adminRows)
        {
            IntentTrainingResult? result = null;
            string? error = null;
            try
            {
                var rows = IntentModelTrainer.ReadCsv(BaseDataPath!);
                // Câu admin thêm: nhân 3 lần để mô hình "chú ý" hơn (vì ít so với 100.000 câu có sẵn)
                for (var i = 0; i < 3; i++) rows.AddRange(adminRows);
                var hand = HandTestPath != null ? IntentModelTrainer.ReadCsv(HandTestPath) : new List<IntentInput>();
                result = await Task.Run(() => IntentModelTrainer.Train(rows, hand, ModelPath));
                _logger.LogInformation("Chatbot: huấn luyện xong lần #{Id}, độ chính xác {Accuracy:P2}", runId, result.MicroAccuracy);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logger.LogError(ex, "Chatbot: huấn luyện lần #{Id} bị lỗi", runId);
            }
            finally
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IChatbotAdminRepository>();
                    var run = await repository.GetTrainingAsync(runId);
                    if (run != null)
                    {
                        run.FinishedAt = DateTime.UtcNow;
                        run.Status = error == null ? "Succeeded" : "Failed";
                        run.MicroAccuracy = result?.MicroAccuracy;
                        run.MacroAccuracy = result?.MacroAccuracy;
                        run.HandTestAccuracy = result?.HandTestAccuracy;
                        run.HandTestCount = result?.HandTestCount;
                        run.Message = error ?? (ModelLoaded
                            ? "Đã lưu mô hình mới, bot dùng ngay."
                            : "Đã lưu mô hình mới. Server chưa nạp mô hình lúc khởi động -> hãy chạy lại server để bot dùng mô hình.");
                        if (run.Message.Length > 1000) run.Message = run.Message[..1000];
                        await repository.UpdateTrainingAsync(run);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chatbot: không cập nhật được kết quả huấn luyện #{Id}", runId);
                }
                _isTraining = false;
                Gate.Release();
            }
        }

        private static string? FindFile(string root, params string?[] candidates)
        {
            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;
                var path = Path.GetFullPath(Path.IsPathRooted(c) ? c : Path.Combine(root, c));
                if (File.Exists(path)) return path;
            }
            return null;
        }
    }
}
