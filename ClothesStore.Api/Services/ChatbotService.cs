// [THÊM MỚI] Chatbot AI - "bộ não" trả lời câu hỏi của khách
using System.Globalization;
using System.Text;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Services.Chatbot;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ML;

namespace ClothesStore.Api.Services
{
    /*
     * CÁCH BOT TRẢ LỜI 1 CÂU HỎI:
     *   1. Mô hình ML (tự train, file MLModels/intent-model.zip) đoán khách MUỐN GÌ (intent) + độ tin cậy.
     *   2. EntityExtractor lấy thông tin trong câu: danh mục, màu, size, giá, chiều cao, cân nặng, mã đơn...
     *   3. Tùy intent, truy vấn database (ChatbotRepository) + luật tư vấn (ChatbotKnowledge) để soạn câu trả lời.
     *   4. Trả về câu trả lời + sản phẩm gợi ý + đường dẫn + câu hỏi gợi ý + ngữ cảnh cho câu sau.
     * Không gọi bất kỳ API AI bên ngoài nào.
     */
    public class ChatbotService : IChatbotService
    {
        /// <summary>Mô hình chắc chắn dưới mức này thì không tin, đoán lại theo thông tin trong câu</summary>
        private const float MinConfidence = 0.5f;
        private const string VocabularyCacheKey = "chatbot-vocabulary";
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        private readonly IChatbotRepository _repository;
        private readonly IServiceProvider _services;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(IChatbotRepository repository, IServiceProvider services, IMemoryCache cache, ILogger<ChatbotService> logger)
        {
            _repository = repository;
            _services = services;
            _cache = cache;
            _logger = logger;
        }

        /* Thông tin của 1 lượt hỏi, truyền cho các hàm trả lời bên dưới */
        private sealed class Turn
        {
            public required string Message { get; init; }
            public required string Plain { get; init; }              // câu đã bỏ dấu (TextNormalizer.ForMatching)
            public required ChatEntities Entities { get; init; }
            public required ChatContextDto Context { get; init; }    // ngữ cảnh câu trước
            public required ChatVocabulary Vocab { get; init; }
            public required ChatResponseDto Response { get; init; }
            public string? UserId { get; init; }

            public bool Says(params string[] words) => words.Any(w => Plain.Contains(" " + w + " "));
        }

        // ======================================================================================== HÀM CHÍNH
        public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, string? applicationUserId)
        {
            var message = (request.Message ?? "").Trim();
            var vocab = await GetVocabularyAsync();
            var entities = EntityExtractor.Extract(message, vocab);
            var context = request.Context ?? new ChatContextDto();

            var (intent, confidence) = PredictIntent(message);
            if (confidence < MinConfidence) intent = GuessFromEntities(entities) ?? ChatIntents.KhongHieu;

            var turn = new Turn
            {
                Message = message,
                Plain = TextNormalizer.ForMatching(message),
                Entities = entities,
                Context = context,
                Vocab = vocab,
                Response = new ChatResponseDto { Intent = intent, Confidence = confidence },
                UserId = applicationUserId
            };

            switch (intent)
            {
                case ChatIntents.TimSanPham: await AnswerFindProductsAsync(turn); break;
                case ChatIntents.TuVanSize: await AnswerSizeAsync(turn); break;
                case ChatIntents.TuVanDip: await AnswerOccasionAsync(turn); break;
                case ChatIntents.TuVanThoiTiet: await AnswerWeatherAsync(turn); break;
                case ChatIntents.PhoiDo: await AnswerOutfitAsync(turn); break;
                case ChatIntents.KhuyenMai: AnswerPromotion(turn); break;
                case ChatIntents.ConHang: await AnswerStockAsync(turn); break;
                case ChatIntents.CuaHang: AnswerStores(turn); break;
                case ChatIntents.ChinhSach: AnswerPolicy(turn); break;
                case ChatIntents.DonHang: await AnswerOrdersAsync(turn); break;
                case ChatIntents.ChaoHoi: turn.Response.Reply = Pick(ChatbotKnowledge.Greetings); break;
                case ChatIntents.CamOn: turn.Response.Reply = Pick(ChatbotKnowledge.Thanks); break;
                case ChatIntents.NgoaiPhamVi: turn.Response.Reply = ChatbotKnowledge.OutOfScope; break;
                default: turn.Response.Reply = ChatbotKnowledge.NotUnderstood; break;
            }

            // Ngữ cảnh cho câu hỏi sau
            var response = turn.Response;
            response.Context.LastIntent = intent;
            response.Context.CategoryId ??= entities.CategoryIds.Count > 0 ? entities.CategoryIds[0] : context.CategoryId;
            response.Context.Colors = entities.Colors.Count > 0 ? entities.Colors : context.Colors;
            response.Context.ProductId ??= context.ProductId;
            if (response.Suggestions.Count == 0) response.Suggestions.AddRange(ChatbotKnowledge.DefaultSuggestions);
            return response;
        }

        // ======================================================================================== MÔ HÌNH ML
        /*
         * Đoán intent bằng mô hình đã train. PredictionEnginePool (đăng ký ở Program.cs) cho phép nhiều người chat
         * cùng lúc và tự nạp lại khi file .zip được thay (train lại).
         * Nếu chưa có file mô hình / lỗi -> đoán tạm bằng từ khóa để bot vẫn chạy được.
         */
        private (string Intent, float Confidence) PredictIntent(string message)
        {
            try
            {
                var pool = _services.GetService<PredictionEnginePool<IntentInput, IntentPrediction>>();
                if (pool != null)
                {
                    var prediction = pool.Predict(ChatIntents.ModelName, new IntentInput { Text = TextNormalizer.ForModel(message) });
                    var confidence = prediction.Score.Length > 0 ? prediction.Score.Max() : 0f;
                    return (prediction.PredictedLabel, confidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chatbot: không dùng được mô hình intent (MLModels/intent-model.zip), chuyển sang đoán theo từ khóa.");
            }

            var plain = TextNormalizer.ForMatching(message);
            foreach (var (intent, keywords) in ChatbotKnowledge.KeywordFallback)
                if (keywords.Any(k => plain.Contains(" " + k.Trim() + " ")))
                    return (intent, MinConfidence);
            return (ChatIntents.KhongHieu, 0f);
        }

        /* Mô hình không chắc chắn -> dựa vào thông tin trong câu để đoán */
        private static string? GuessFromEntities(ChatEntities e)
        {
            if (e.HeightCm != null || e.WeightKg != null) return ChatIntents.TuVanSize;
            if (e.OrderId != null) return ChatIntents.DonHang;
            if (e.PromoCode != null) return ChatIntents.KhuyenMai;
            if (e.Occasion != null) return ChatIntents.TuVanDip;
            if (e.Weather != null) return ChatIntents.TuVanThoiTiet;
            if (e.HasProductFilter) return ChatIntents.TimSanPham;
            if (e.Places.Count > 0) return ChatIntents.CuaHang;
            return null;
        }

        // ======================================================================================== 1. TÌM SẢN PHẨM
        private async Task AnswerFindProductsAsync(Turn t)
        {
            var e = t.Entities;
            var categoryIds = CategoriesOf(t);
            var query = new ChatProductQuery
            {
                CategoryIds = Expand(t.Vocab, categoryIds),
                Colors = e.Colors,
                Sizes = e.Sizes,
                MinPrice = e.MinPrice,
                MaxPrice = e.MaxPrice,
                CollectionTechIds = e.CollectionTechIds,
                StoreIds = StoreIdsOf(t),
                InStockOnly = true
            };

            if (query.CategoryIds.Count == 0 && !e.HasProductFilter)
            {
                var (best, _) = await _repository.SearchProductsAsync(new ChatProductQuery { InStockOnly = true });
                t.Response.Reply = "Bạn muốn tìm loại nào: áo thun, sơ mi, áo khoác, quần jeans, quần tây, quần short hay phụ kiện? "
                    + "Bạn có thể nói thêm màu, size, khoảng giá. Trong lúc chờ, đây là vài mẫu đang bán chạy:";
                t.Response.Products = best;
                t.Response.Suggestions.AddRange(new[] { "Tìm áo thun trắng dưới 300k", "Áo khoác chống nước", "Quần jeans size 31", "Sơ mi đi làm" });
                return;
            }

            var (items, total) = await _repository.SearchProductsAsync(query);
            var note = "";
            // Không có kết quả -> nới điều kiện dần: bỏ màu / size / công nghệ, rồi bỏ giá
            if (total == 0 && (query.Colors.Count > 0 || query.Sizes.Count > 0 || query.CollectionTechIds.Count > 0 || query.StoreIds.Count > 0))
            {
                query.Colors = new(); query.Sizes = new(); query.CollectionTechIds = new(); query.StoreIds = new();
                (items, total) = await _repository.SearchProductsAsync(query);
                note = "Không có mẫu nào đúng hết các yêu cầu, mình gợi ý các mẫu gần nhất (bỏ bớt màu / size / chất liệu). ";
            }
            if (total == 0 && (query.MinPrice != null || query.MaxPrice != null))
            {
                query.MinPrice = null; query.MaxPrice = null;
                (items, total) = await _repository.SearchProductsAsync(query);
                note = "Không có mẫu nào trong khoảng giá này, đây là các mẫu cùng loại ở mức giá khác. ";
            }

            var description = Describe(t, categoryIds);
            t.Response.Reply = total == 0
                ? $"Tiếc quá, hiện shop chưa có {description} còn hàng. Bạn thử tìm loại khác hoặc bỏ bớt điều kiện nhé."
                : $"{note}Mình tìm được {total} {description}. Đây là {items.Count} mẫu nổi bật:";
            t.Response.Products = items;
            if (categoryIds.Count > 0)
                t.Response.Links.Add(new ChatLinkDto { Text = "Xem tất cả", Url = $"/shop/products.html?category={categoryIds[0]}" });
            t.Response.Suggestions.AddRange(new[] { "Còn size L không?", "Có màu khác không?", "Mặc size gì cho vừa?", "Phối với gì đẹp?" });
        }

        // ======================================================================================== 2. TƯ VẤN SIZE
        private async Task AnswerSizeAsync(Turn t)
        {
            var e = t.Entities;
            var categoryIds = CategoriesOf(t);
            var askedPants = t.Says("quan") || categoryIds.Any(id => IsUnder(t.Vocab, id, ChatbotKnowledge.BottomRoots));
            // Không nói loại sản phẩm -> mặc định áo thun (hoặc quần jeans nếu khách nói "quần")
            var categoryId = categoryIds.Count > 0 ? categoryIds[0]
                : CategoryIdByName(t.Vocab, askedPants ? "Quần Jeans Nguyên Bản" : "Áo Thun Cổ Tròn");
            if (categoryId == null) { t.Response.Reply = "Bạn muốn xem size của sản phẩm nào ạ?"; return; }

            var (chartCategoryId, rows) = await FindSizeChartAsync(t.Vocab, categoryId.Value);
            var name = CategoryName(t.Vocab, chartCategoryId).ToLower(Vi);
            t.Response.Context.CategoryId = categoryId;

            if (rows.Count == 0)
            {
                t.Response.Reply = $"Sản phẩm {CategoryName(t.Vocab, categoryId.Value).ToLower(Vi)} hiện chưa có bảng size (thường là Free Size, vừa hầu hết mọi người).";
                return;
            }

            var sb = new StringBuilder();
            if (e.HeightCm == null && e.WeightKg == null)
            {
                sb.AppendLine($"Bảng size {name}:");
                foreach (var r in rows) sb.AppendLine($"• {r.SizeLabel}: {r.Measurements}");
                if (SizeAdvisor.CanAdvise(rows)) sb.Append("Bạn cho mình biết chiều cao và cân nặng (ví dụ \"1m70 65kg\") để mình chọn size giúp nhé.");
                t.Response.Reply = sb.ToString().TrimEnd();
                t.Response.Suggestions.AddRange(new[] { "Cao 1m70 nặng 65kg", "Cao 1m65 nặng 58kg", "Cao 1m75 nặng 72kg" });
                return;
            }

            var recommendation = SizeAdvisor.Recommend(rows, e.HeightCm, e.WeightKg);
            if (recommendation == null)
            {
                // Bảng size không có chiều cao / cân nặng (giày, nón...) hoặc quần mà khách chỉ nói chiều cao
                sb.AppendLine(SizeAdvisor.CanAdvise(rows)
                    ? $"Size {name} chọn theo cân nặng, bạn cho mình thêm cân nặng nhé. Bảng size:"
                    : $"Size {name} chọn theo số đo dưới đây (không theo chiều cao, cân nặng):");
                foreach (var r in rows) sb.AppendLine($"• {r.SizeLabel}: {r.Measurements}");
                t.Response.Reply = sb.ToString().TrimEnd();
                return;
            }

            var body = string.Join(", ", new[] { e.HeightCm != null ? $"cao {e.HeightCm}cm" : null, e.WeightKg != null ? $"nặng {e.WeightKg}kg" : null }.Where(x => x != null));
            sb.AppendLine($"Với {body}, bạn hợp {name} size {recommendation.SizeLabel} ({recommendation.Measurements}).");
            if (!recommendation.FitsWell) sb.AppendLine("Số đo của bạn hơi lệch so với bảng size, đây là size gần nhất.");
            if (recommendation.AlternativeSize != null)
                sb.AppendLine($"Chiều cao và cân nặng của bạn rơi vào 2 size khác nhau, nếu thích mặc rộng thoải mái hãy cân nhắc size {recommendation.AlternativeSize}.");

            // Không nói rõ loại -> tư vấn thêm size quần cho tiện
            if (categoryIds.Count == 0 && !askedPants && e.WeightKg != null)
            {
                var pantsId = CategoryIdByName(t.Vocab, "Quần Jeans Nguyên Bản");
                if (pantsId != null)
                {
                    var pantsRows = await _repository.GetSizeChartAsync(pantsId.Value);
                    var pants = SizeAdvisor.Recommend(pantsRows, e.HeightCm, e.WeightKg);
                    if (pants != null) sb.AppendLine($"Nếu mua quần jeans / kaki (size số), bạn hợp size {pants.SizeLabel}.");
                }
            }
            sb.Append("Bảng size chỉ để tham khảo, form từng mẫu có thể rộng / ôm khác nhau một chút.");
            t.Response.Reply = sb.ToString();
            t.Response.Suggestions.AddRange(new[] { $"Tìm {name} size {recommendation.SizeLabel}", "Xem bảng size quần jeans", "Phối đồ thế nào?" });
        }

        /* Bảng size của danh mục; danh mục cha không có thì lấy của danh mục con đầu tiên, rồi tới danh mục cha */
        private async Task<(int CategoryId, List<ChatSizeRow> Rows)> FindSizeChartAsync(ChatVocabulary vocab, int categoryId)
        {
            var candidates = new List<int> { categoryId };
            candidates.AddRange(Expand(vocab, new List<int> { categoryId }).Where(id => id != categoryId));
            var parent = vocab.Categories.FirstOrDefault(c => c.Id == categoryId)?.ParentId;
            if (parent != null) candidates.Add(parent.Value);

            foreach (var id in candidates)
            {
                var rows = await _repository.GetSizeChartAsync(id);
                if (rows.Count > 0) return (id, rows);
            }
            return (categoryId, new List<ChatSizeRow>());
        }

        // ======================================================================================== 3. TƯ VẤN THEO DỊP
        private async Task AnswerOccasionAsync(Turn t)
        {
            var rule = t.Entities.Occasion;
            if (rule == null)
            {
                t.Response.Reply = "Bạn định mặc cho dịp nào: đi làm, đi tiệc / đám cưới, đi chơi, đi biển, đi Đà Lạt, tập gym hay đi học? Mình sẽ gợi ý set đồ phù hợp.";
                t.Response.Suggestions.AddRange(new[] { "Đi làm văn phòng mặc gì?", "Đi đám cưới mặc gì?", "Đi biển mặc gì?", "Đi hẹn hò mặc gì?" });
                return;
            }
            t.Response.Reply = $"Gợi ý cho dịp {rule.Title}:\n{rule.Tip}\nMột vài món hợp với bạn:";
            t.Response.Products = await SuggestByRuleAsync(t, rule);
            t.Response.Suggestions.AddRange(new[] { "Mặc size gì cho vừa?", "Có mã giảm giá không?", "Phối màu thế nào?" });
        }

        // ======================================================================================== 4. THỜI TIẾT / CHẤT LIỆU
        private async Task AnswerWeatherAsync(Turn t)
        {
            var e = t.Entities;
            var techs = e.CollectionTechNames.Where(n => ChatbotKnowledge.TechDescriptions.ContainsKey(n)).ToList();

            // Hỏi định nghĩa: "vải kháng khuẩn là gì", "công nghệ co giãn có tác dụng gì"
            if (techs.Count > 0 && t.Says("la gi", "tac dung", "khac gi", "la sao", "nhu the nao", "co tot khong", "the nao"))
            {
                var sb = new StringBuilder();
                foreach (var tech in techs) sb.AppendLine($"• {tech}: {ChatbotKnowledge.TechDescriptions[tech]}");
                sb.Append("Một vài sản phẩm có công nghệ này:");
                t.Response.Reply = sb.ToString();
                (t.Response.Products, _) = await _repository.SearchProductsAsync(new ChatProductQuery
                {
                    CollectionTechIds = e.CollectionTechIds, CategoryIds = Expand(t.Vocab, e.CategoryIds), InStockOnly = true, Take = 4
                });
                return;
            }

            var rule = e.Weather;
            if (rule == null && techs.Count > 0)
            {
                // Chỉ nói tên công nghệ: "có áo nào vải mát không"
                t.Response.Reply = $"Đây là các mẫu có công nghệ {string.Join(", ", techs)}:";
                (t.Response.Products, _) = await _repository.SearchProductsAsync(new ChatProductQuery
                {
                    CollectionTechIds = e.CollectionTechIds, CategoryIds = Expand(t.Vocab, e.CategoryIds), InStockOnly = true
                });
                return;
            }
            if (rule == null)
            {
                t.Response.Reply = "Bạn đang cần đồ cho thời tiết thế nào: trời nóng, trời mưa, trời lạnh, hay cần vải ít nhăn / thấm hút mồ hôi? Mình sẽ gợi ý chất liệu phù hợp.";
                t.Response.Suggestions.AddRange(new[] { "Trời nóng mặc gì cho mát?", "Áo khoác đi mưa", "Vải nào không nhăn?", "Đi Đà Lạt mặc gì?" });
                return;
            }

            var techText = string.Join(", ", rule.CollectionTechs.Where(ChatbotKnowledge.TechDescriptions.ContainsKey));
            t.Response.Reply = $"Với {rule.Title}: {rule.Tip}" + (techText.Length > 0 ? $"\nCông nghệ vải nên chọn: {techText}." : "") + "\nGợi ý cho bạn:";
            t.Response.Products = await SuggestByRuleAsync(t, rule);
            t.Response.Suggestions.AddRange(new[] { "Vải mát là gì?", "Mặc size gì cho vừa?", "Có mã giảm giá không?" });
        }

        /* Gợi ý sản phẩm theo 1 luật: lấy 2 mẫu / danh mục (tối đa 6), ưu tiên bộ sưu tập / công nghệ của luật */
        private async Task<List<ChatProductDto>> SuggestByRuleAsync(Turn t, ChatbotKnowledge.AdviceRule rule)
        {
            var preferIds = t.Vocab.CollectionTechs.Where(x => rule.CollectionTechs.Contains(x.Name)).Select(x => x.Id).ToList();
            // Khách có nói loại sản phẩm trong CÂU NÀY thì chỉ gợi ý loại đó (không lấy theo ngữ cảnh câu trước)
            var groups = t.Entities.CategoryIds.Select(id => new List<int> { id }).ToList();
            if (groups.Count == 0)
                groups = rule.Categories.Select(n => CategoryIdByName(t.Vocab, n)).Where(id => id != null).Select(id => new List<int> { id!.Value }).ToList();

            var result = new List<ChatProductDto>();
            var perGroup = groups.Count <= 1 ? 6 : 2;
            foreach (var group in groups)
            {
                if (result.Count >= 6) break;
                var (items, _) = await _repository.SearchProductsAsync(new ChatProductQuery
                {
                    CategoryIds = Expand(t.Vocab, group),
                    PreferCollectionTechIds = preferIds,
                    Colors = t.Entities.Colors,
                    MinPrice = t.Entities.MinPrice,
                    MaxPrice = t.Entities.MaxPrice,
                    InStockOnly = true,
                    Take = perGroup
                });
                result.AddRange(items.Where(p => result.All(r => r.Id != p.Id)));
            }
            return result.Take(6).ToList();
        }

        // ======================================================================================== 5. PHỐI ĐỒ
        private async Task AnswerOutfitAsync(Turn t)
        {
            var e = t.Entities;
            var categoryIds = CategoriesOf(t);
            var baseId = categoryIds.Count > 0 ? categoryIds[0] : (int?)null;
            var baseName = baseId != null ? CategoryName(t.Vocab, baseId.Value).ToLower(Vi) : "món đồ của bạn";
            var isTop = baseId != null && IsUnder(t.Vocab, baseId.Value, ChatbotKnowledge.TopRoots);
            var isBottom = baseId != null && IsUnder(t.Vocab, baseId.Value, ChatbotKnowledge.BottomRoots);

            var colorWord = e.ColorWords.FirstOrDefault();
            var matchKey = colorWord == null ? null
                : ChatbotKnowledge.ColorMatches.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => colorWord == k || colorWord.StartsWith(k + " "));
            var matchColors = matchKey != null ? ChatbotKnowledge.ColorMatches[matchKey] : new[] { "Trắng", "Đen", "Be", "Xanh Navy" };
            // Khách nói đúng 1 màu ("xanh navy") thì ghi tên màu đó; nói chung chung ("xanh") thì ghi đúng chữ khách nói
            var colorLabel = colorWord == null ? "" : e.Colors.Count == 1 ? $" {e.Colors[0].ToLower(Vi)}" : $" {colorWord}";

            var sb = new StringBuilder();
            sb.AppendLine($"{Capitalize(baseName)}{colorLabel} dễ phối với các màu: {string.Join(", ", matchColors)}.");
            if (isTop) sb.AppendLine("• Phía dưới: quần jeans hoặc kaki cho phong cách thường ngày, quần tây nếu đi làm / đi tiệc, quần short khi đi chơi trời nóng.");
            else if (isBottom) sb.AppendLine("• Phía trên: áo thun trơn hoặc polo cho thoải mái, sơ mi để lịch sự hơn, khoác thêm áo khoác khi trời lạnh.");
            else sb.AppendLine("• Một bộ cơ bản: áo (thun / polo / sơ mi) + quần (jeans / kaki / tây) + giày, chọn 1 món làm điểm nhấn.");
            sb.AppendLine("• Mẹo: không dùng quá 3 màu trong 1 bộ; màu trung tính (trắng, đen, be, xám, navy) phối với nhau gần như không bao giờ sai.");
            sb.Append("Một vài món bạn có thể phối cùng:");
            t.Response.Reply = sb.ToString();

            // Gợi ý món còn thiếu (có áo -> gợi ý quần và ngược lại) theo các màu hợp
            var roots = isTop ? ChatbotKnowledge.BottomRoots : isBottom ? ChatbotKnowledge.TopRoots : ChatbotKnowledge.TopRoots.Concat(ChatbotKnowledge.BottomRoots).ToArray();
            var targetIds = Expand(t.Vocab, roots.Select(n => CategoryIdByName(t.Vocab, n)).Where(id => id != null).Select(id => id!.Value).ToList());
            var dbColors = t.Vocab.Colors.Where(c => matchColors.Contains(c)).ToList();
            (t.Response.Products, _) = await _repository.SearchProductsAsync(new ChatProductQuery
            {
                CategoryIds = targetIds, Colors = dbColors, MaxPrice = e.MaxPrice, InStockOnly = true
            });
            t.Response.Suggestions.AddRange(new[] { "Đi làm nên phối thế nào?", "Mặc size gì cho vừa?", "Tìm giày hợp bộ này" });
        }

        // ======================================================================================== 6. KHUYẾN MÃI
        private static void AnswerPromotion(Turn t)
        {
            var now = DateTime.UtcNow; // backend lưu và so sánh ngày khuyến mãi theo giờ UTC
            var promotions = t.Vocab.Promotions;
            t.Response.Links.Add(new ChatLinkDto { Text = "Xem tất cả ưu đãi", Url = "/shop/promotions.html" });

            if (t.Entities.PromoCode != null)
            {
                var code = t.Entities.PromoCode;
                var p = promotions.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
                t.Response.Reply = p == null ? $"Mã {code} không tồn tại. Bạn kiểm tra lại chính tả hoặc xem các mã đang chạy nhé."
                    : p.EndDate < now ? $"Mã {p.Code} đã hết hạn từ ngày {VnDate(p.EndDate)}."
                    : p.StartDate > now ? $"Mã {p.Code} chưa đến thời gian áp dụng, bắt đầu từ ngày {VnDate(p.StartDate)}."
                    : $"Mã {p.Code} đang dùng được đến hết ngày {VnDate(p.EndDate)}, giảm {Money(p.DiscountValue)} (tối đa bằng tiền hàng). Bạn nhập mã ở bước Giỏ hàng hoặc Thanh toán nhé.";
                return;
            }

            var active = promotions.Where(p => p.StartDate <= now && p.EndDate >= now).OrderByDescending(p => p.DiscountValue).ToList();
            if (active.Count == 0)
            {
                t.Response.Reply = "Hiện chưa có mã khuyến mãi nào đang chạy. Bạn quay lại sau nhé!";
                return;
            }
            var sb = new StringBuilder($"Đang có {active.Count} mã khuyến mãi, giảm nhiều nhất:\n");
            foreach (var p in active.Take(5)) sb.AppendLine($"• {p.Code}: giảm {Money(p.DiscountValue)}, hạn đến {VnDate(p.EndDate)}");
            sb.Append("Mỗi đơn dùng 1 mã, giảm thẳng vào tổng tiền (tối đa bằng tiền hàng). Nhập mã ở bước Giỏ hàng hoặc Thanh toán.");
            t.Response.Reply = sb.ToString();
            t.Response.Suggestions.AddRange(active.Take(2).Select(p => $"Mã {p.Code} còn dùng được không?"));
        }

        // ======================================================================================== 7. CÒN HÀNG
        private async Task AnswerStockAsync(Turn t)
        {
            var e = t.Entities;
            var storeIds = StoreIdsOf(t);
            var placeText = e.Places.Count > 0 ? " ở khu vực bạn hỏi" : "";

            // Khách đang xem 1 sản phẩm cụ thể (giao diện gửi ProductId lên) và không nhắc loại khác
            if (t.Context.ProductId is int productId && e.CategoryIds.Count == 0)
            {
                var product = await _repository.GetProductAsync(productId);
                if (product != null)
                {
                    var colors = e.Colors.Count > 0 ? e.Colors : new List<string>();
                    var rows = await _repository.GetProductStockAsync(productId, colors, e.Sizes, storeIds);
                    var what = product.Name + (e.Colors.Count > 0 ? $" màu {string.Join("/", e.Colors)}" : "") + (e.Sizes.Count > 0 ? $" size {string.Join("/", e.Sizes)}" : "");
                    if (rows.Count == 0)
                    {
                        t.Response.Reply = $"Tiếc quá, {what} hiện đã hết hàng{placeText}. Bạn xem thử các mẫu tương tự nhé:";
                        (t.Response.Products, _) = await _repository.SearchProductsAsync(new ChatProductQuery
                        {
                            CategoryIds = new List<int> { product.CategoryId }, Sizes = e.Sizes, InStockOnly = true, Take = 4
                        });
                        return;
                    }
                    var sb = new StringBuilder($"{what} còn {rows.Sum(r => r.Quantity)} sản phẩm{placeText}.\n");
                    sb.AppendLine("• Size còn hàng: " + string.Join(", ", rows.Select(r => r.Size).Distinct()));
                    sb.AppendLine("• Màu còn hàng: " + string.Join(", ", rows.Select(r => r.Color).Distinct()));
                    sb.AppendLine("Cửa hàng còn nhiều nhất:");
                    foreach (var s in rows.GroupBy(r => new { r.StoreName, r.Location }).Select(g => new { g.Key.StoreName, g.Key.Location, Qty = g.Sum(x => x.Quantity) })
                                 .OrderByDescending(x => x.Qty).Take(5))
                        sb.AppendLine($"• {s.StoreName} ({s.Location}): còn {s.Qty}");
                    t.Response.Reply = sb.ToString().TrimEnd();
                    t.Response.Context.CategoryId = product.CategoryId;
                    return;
                }
            }

            var categoryIds = CategoriesOf(t);
            if (categoryIds.Count == 0 && !e.HasProductFilter)
            {
                t.Response.Reply = "Bạn muốn kiểm tra còn hàng sản phẩm nào ạ? Bạn có thể mở trang sản phẩm rồi hỏi mình, hoặc nói tên loại, màu, size (ví dụ \"áo khoác dù đen size L còn không\").";
                return;
            }
            var (items, total) = await _repository.SearchProductsAsync(new ChatProductQuery
            {
                CategoryIds = Expand(t.Vocab, categoryIds), Colors = e.Colors, Sizes = e.Sizes,
                MinPrice = e.MinPrice, MaxPrice = e.MaxPrice, StoreIds = storeIds, InStockOnly = true
            });
            var description = Describe(t, categoryIds);
            t.Response.Reply = total == 0
                ? $"Hiện {description} đã hết hàng{placeText}. Bạn thử màu / size khác nhé."
                : $"Có {total} {description} còn hàng{placeText}. Một vài mẫu:";
            t.Response.Products = items;
        }

        // ======================================================================================== 8. CỬA HÀNG
        private static void AnswerStores(Turn t)
        {
            var stores = t.Vocab.Stores.Where(s => !s.Name.Contains("Kho", StringComparison.OrdinalIgnoreCase)).ToList();
            t.Response.Links.Add(new ChatLinkDto { Text = "Xem hệ thống cửa hàng", Url = "/shop/stores.html" });
            var sb = new StringBuilder();
            if (t.Says("mo cua", "dong cua", "may gio", "gio mo"))
                sb.AppendLine("Giờ mở cửa của từng chi nhánh hiện chưa có trên hệ thống, bạn vui lòng liên hệ trực tiếp cửa hàng nhé.");

            if (t.Entities.Places.Count > 0)
            {
                var found = stores.Where(s => t.Entities.Places.Any(p => EntityExtractor.Clean(s.Location).Contains(p))).ToList();
                if (found.Count == 0)
                    sb.Append("Hiện shop chưa có cửa hàng ở khu vực này. Bạn có thể đặt online (miễn phí vận chuyển) hoặc xem các chi nhánh khác.");
                else
                {
                    sb.AppendLine($"Có {found.Count} cửa hàng ở khu vực bạn hỏi:");
                    foreach (var s in found.Take(6)) sb.AppendLine($"• {s.Name}: {s.Location}");
                    if (found.Count > 6) sb.Append($"... và {found.Count - 6} cửa hàng khác.");
                }
            }
            else
            {
                var byCity = stores.GroupBy(s => s.Location.Split(',').Last().Trim()).OrderByDescending(g => g.Count()).Take(6);
                sb.AppendLine($"Shop có {stores.Count} cửa hàng trên toàn quốc:");
                foreach (var g in byCity) sb.AppendLine($"• {g.Key}: {g.Count()} cửa hàng");
                sb.Append("Bạn đang ở quận / thành phố nào để mình chỉ cửa hàng gần nhất?");
                t.Response.Suggestions.AddRange(new[] { "Cửa hàng ở Quận 1", "Cửa hàng ở Hà Nội", "Cửa hàng ở Đà Nẵng" });
            }
            t.Response.Reply = sb.ToString().TrimEnd();
        }

        // ======================================================================================== 9. CHÍNH SÁCH
        private static void AnswerPolicy(Turn t)
        {
            if (t.Says("doi tra", "tra hang", "doi hang", "hoan tien", "doi size", "tra lai", "cat mac"))
                t.Response.Reply = ChatbotKnowledge.ReturnPolicy;
            else if (t.Says("diem", "tich diem", "thanh vien", "hang thanh vien", "vip", "len hang", "hang vang", "hang bac", "hang dong", "hang vip"))
                t.Response.Reply = LoyaltyPolicy(t.Vocab);
            else if (t.Says("thanh toan", "momo", "vnpay", "cod", "chuyen khoan", "tra tien", "the ngan hang"))
                t.Response.Reply = ChatbotKnowledge.PaymentPolicy;
            else if (t.Says("ship", "giao", "giao hang", "van chuyen", "phi ship", "freeship", "mien phi"))
                t.Response.Reply = ChatbotKnowledge.ShippingPolicy;
            else if (t.Says("danh gia", "review"))
                t.Response.Reply = ChatbotKnowledge.ReviewPolicy;
            else if (t.Says("tai khoan", "dang ky", "mat khau", "dang nhap"))
                t.Response.Reply = ChatbotKnowledge.AccountPolicy;
            else
            {
                t.Response.Reply = "Bạn muốn hỏi chính sách nào: trả hàng - hoàn tiền, tích điểm - hạng thành viên, thanh toán hay giao hàng?";
                t.Response.Suggestions.AddRange(new[] { "Chính sách đổi trả", "Tích điểm thế nào?", "Có những cách thanh toán nào?", "Phí ship bao nhiêu?" });
            }
        }

        private static string LoyaltyPolicy(ChatVocabulary vocab)
        {
            var sb = new StringBuilder("Tích điểm và hạng thành viên:\n");
            sb.AppendLine($"• Mỗi {Money(LoyaltyRules.AmountPerPoint)} trong đơn hàng hoàn tất = 1 điểm (cộng tự động khi đơn giao thành công).");
            sb.AppendLine("• Trả hàng thì điểm của phần hàng trả bị trừ lại.");
            if (vocab.Tiers.Count > 0)
                sb.AppendLine("• Các hạng: " + string.Join(", ", vocab.Tiers.Select(x => $"{x.TierName} (từ {x.MinPoints.ToString("N0", Vi)} điểm)")) + ". Hạng tự nâng khi đủ điểm.");
            sb.Append("Bạn xem điểm và lịch sử điểm trong Tài khoản → Điểm & hạng thành viên.");
            return sb.ToString();
        }

        // ======================================================================================== 10. ĐƠN HÀNG
        private async Task AnswerOrdersAsync(Turn t)
        {
            var customerId = t.UserId != null ? await _repository.GetCustomerIdByUserIdAsync(t.UserId) : null;
            if (customerId == null)
            {
                t.Response.Reply = "Để xem đơn hàng, bạn cần đăng nhập bằng tài khoản khách hàng nhé.";
                t.Response.Links.Add(new ChatLinkDto { Text = "Đăng nhập", Url = "/shop/login.html" });
                return;
            }

            var orderId = t.Entities.OrderId;
            var orders = await _repository.GetOrdersAsync(customerId.Value, orderId, orderId != null ? 1 : 3);
            if (orders.Count == 0)
            {
                t.Response.Reply = orderId != null
                    ? $"Mình không tìm thấy đơn #{orderId} trong tài khoản của bạn. Bạn kiểm tra lại mã đơn nhé."
                    : "Bạn chưa có đơn hàng nào. Ghé xem sản phẩm và đặt đơn đầu tiên nhé!";
                t.Response.Links.Add(new ChatLinkDto { Text = "Đơn hàng của tôi", Url = "/shop/account/orders.html" });
                return;
            }

            var sb = new StringBuilder(orderId != null ? "" : "Các đơn hàng gần nhất của bạn:\n");
            foreach (var o in orders)
            {
                sb.Append($"• Đơn #{o.Id} ({VnDate(o.OrderDate)}): {OrderStatusText(o.Status)}, {o.ItemCount} sản phẩm, {Money(o.TotalAmount)}");
                if (o.TrackingNumber != null) sb.Append($". Vận chuyển: {o.Carrier} - mã vận đơn {o.TrackingNumber} ({ShipmentStatusText(o.ShipmentStatus)})");
                sb.AppendLine();
                t.Response.Links.Add(new ChatLinkDto { Text = $"Chi tiết đơn #{o.Id}", Url = $"/shop/account/order-detail.html?id={o.Id}" });
            }

            if (t.Says("huy", "huy don"))
                sb.AppendLine("Đơn chỉ hủy được khi đang Chờ xác nhận hoặc Đã xác nhận: bạn mở chi tiết đơn và bấm Hủy đơn.");
            if (t.Says("tra hang", "doi tra", "tra lai"))
                sb.AppendLine("Đơn đã giao thành công có thể yêu cầu trả hàng trong 30 ngày kể từ ngày đặt (Tài khoản → Đổi / trả hàng).");
            t.Response.Reply = sb.ToString().TrimEnd();
            t.Response.Links.Add(new ChatLinkDto { Text = "Tất cả đơn hàng", Url = "/shop/account/orders.html" });
        }

        // ======================================================================================== HÀM PHỤ
        private async Task<ChatVocabulary> GetVocabularyAsync()
            => (await _cache.GetOrCreateAsync(VocabularyCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // danh mục, mã KM... ít đổi -> lưu tạm 10 phút
                return _repository.GetVocabularyAsync();
            }))!;

        /* Danh mục khách nhắc tới; câu không nhắc thì lấy theo ngữ cảnh câu trước */
        private static List<int> CategoriesOf(Turn t)
            => t.Entities.CategoryIds.Count > 0 ? t.Entities.CategoryIds
                : t.Context.CategoryId is int id ? new List<int> { id } : new List<int>();

        /* Cửa hàng ở khu vực khách nhắc tới */
        private static List<int> StoreIdsOf(Turn t)
            => t.Entities.Places.Count == 0 ? new List<int>()
                : t.Vocab.Stores.Where(s => t.Entities.Places.Any(p => EntityExtractor.Clean(s.Location).Contains(p))).Select(s => s.Id).ToList();

        /* Thêm toàn bộ danh mục con, cháu ("Áo Khoác" -> Áo Khoác Dù, Áo Khoác Jean...) */
        private static List<int> Expand(ChatVocabulary vocab, List<int> ids)
        {
            var result = new HashSet<int>(ids);
            var queue = new Queue<int>(ids);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var child in vocab.Categories.Where(c => c.ParentId == id))
                    if (result.Add(child.Id)) queue.Enqueue(child.Id);
            }
            return result.ToList();
        }

        /* Danh mục id có nằm dưới 1 trong các danh mục gốc (theo tên) không */
        private static bool IsUnder(ChatVocabulary vocab, int id, string[] rootNames)
        {
            var current = vocab.Categories.FirstOrDefault(c => c.Id == id);
            while (current != null)
            {
                if (rootNames.Contains(current.Name)) return true;
                current = current.ParentId == null ? null : vocab.Categories.FirstOrDefault(c => c.Id == current.ParentId);
            }
            return false;
        }

        private static int? CategoryIdByName(ChatVocabulary vocab, string name)
            => vocab.Categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;

        private static string CategoryName(ChatVocabulary vocab, int id)
            => vocab.Categories.FirstOrDefault(c => c.Id == id)?.Name ?? "sản phẩm";

        /* Mô tả điều kiện tìm bằng lời: "sản phẩm áo khoác màu Đen, dưới 500.000 ₫" */
        private static string Describe(Turn t, List<int> categoryIds)
        {
            var e = t.Entities;
            var parts = new List<string> { categoryIds.Count > 0 ? "mẫu " + string.Join(" / ", categoryIds.Select(id => CategoryName(t.Vocab, id).ToLower(Vi))) : "sản phẩm" };
            if (e.Colors.Count > 0) parts.Add("màu " + string.Join(" / ", e.Colors.Take(3)) + (e.Colors.Count > 3 ? "..." : ""));
            if (e.Sizes.Count > 0) parts.Add("size " + string.Join("/", e.Sizes));
            if (e.CollectionTechNames.Count > 0) parts.Add(string.Join(", ", e.CollectionTechNames));
            if (e.MinPrice != null && e.MaxPrice != null) parts.Add($"giá {Money(e.MinPrice.Value)} - {Money(e.MaxPrice.Value)}");
            else if (e.MaxPrice != null) parts.Add($"dưới {Money(e.MaxPrice.Value)}");
            else if (e.MinPrice != null) parts.Add($"trên {Money(e.MinPrice.Value)}");
            return string.Join(", ", parts);
        }

        private static string Money(decimal value) => value.ToString("N0", Vi) + "đ";

        /* Ngày theo giờ Việt Nam (backend lưu giờ UTC) */
        private static string VnDate(DateTime utc) => utc.AddHours(7).ToString("dd/MM/yyyy", Vi);

        private static string OrderStatusText(string status) => status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Shipping => "Đang giao",
            OrderStatus.Completed => "Hoàn tất",
            OrderStatus.Cancelled => "Đã hủy",
            OrderStatus.Returned => "Đã trả hàng",
            _ => status
        };

        private static string ShipmentStatusText(string? status) => status switch
        {
            ShipmentStatus.Shipping => "đang giao",
            ShipmentStatus.Delivered => "đã giao",
            ShipmentStatus.Failed => "giao thất bại",
            _ => "chưa cập nhật"
        };

        private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0], Vi) + text[1..];

        private static string Pick(string[] options) => options[Random.Shared.Next(options.Length)];
    }
}
