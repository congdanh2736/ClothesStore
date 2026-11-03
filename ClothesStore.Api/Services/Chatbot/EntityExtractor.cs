// [THÊM MỚI] Chatbot AI - lấy thông tin cụ thể trong câu hỏi (danh mục, màu, size, giá, chiều cao, cân nặng...)
using System.Globalization;
using System.Text.RegularExpressions;
using ClothesStore.Api.DTOs.Chatbot;

namespace ClothesStore.Api.Services.Chatbot
{
    /* Các thông tin lấy được trong 1 câu hỏi */
    public class ChatEntities
    {
        public List<int> CategoryIds { get; } = new();          // danh mục được nhắc tới (chưa gồm danh mục con)
        public List<string> Colors { get; } = new();            // tên màu ĐÚNG như trong database ("Xanh Navy")
        public List<string> ColorWords { get; } = new();        // màu khách nói, đã bỏ dấu ("xanh navy", "den") - dùng để phối màu
        public List<string> Sizes { get; } = new();             // "M", "XL", "30"
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public List<int> CollectionTechIds { get; } = new();    // bộ sưu tập / công nghệ vải được nhắc tới
        public List<string> CollectionTechNames { get; } = new();
        public string? PromoCode { get; set; }
        public int? OrderId { get; set; }
        public List<string> Places { get; } = new();            // địa danh đã bỏ dấu ("quan go vap", "ha noi")
        public ChatbotKnowledge.AdviceRule? Occasion { get; set; }
        public ChatbotKnowledge.AdviceRule? Weather { get; set; }

        public bool HasProductFilter => CategoryIds.Count > 0 || Colors.Count > 0 || Sizes.Count > 0
            || MinPrice != null || MaxPrice != null || CollectionTechIds.Count > 0;
    }

    /*
     * Đọc thông tin trong câu bằng TỪ ĐIỂN (lấy từ database) + BIỂU THỨC CHÍNH QUY (regex). Không cần train.
     * Câu được so khớp ở 2 dạng:
     *   - m: đã bỏ dấu, đổi viết tắt   -> khách gõ không dấu vẫn hiểu ("ao khoac den duoi 500k")
     *   - a: giữ nguyên dấu (chữ thường) -> dùng cho từ ngắn dễ trùng nghĩa khi bỏ dấu ("đen"/"đến", "dép"/"đẹp")
     * Phần nào đã nhận ra thì xóa khỏi câu (thay bằng khoảng trắng) để không bị nhận nhầm lần nữa.
     */
    public static class EntityExtractor
    {
        public static ChatEntities Extract(string message, ChatVocabulary vocab)
        {
            var e = new ChatEntities();
            var m = TextNormalizer.ForMatching(message);
            var a = " " + Regex.Replace(Regex.Replace((message ?? "").ToLowerInvariant(), @"[^\p{L}\p{N}#.,\- ]", " "), @"\s+", " ").Trim() + " ";
            var typedAccents = TextNormalizer.HasAccents(message ?? "");

            m = ExtractOrderId(m, e);
            m = ExtractPromoCode(m, vocab, e);
            m = ExtractBody(m, e);
            m = ExtractPrice(m, e);
            m = ExtractSizes(m, message ?? "", e);
            (m, a) = ExtractColors(m, a, typedAccents, vocab, e);
            // Bộ sưu tập dò trên câu TRƯỚC khi xóa tên danh mục, vì có bộ sưu tập chứa tên danh mục ("Sơ Mi Kháng Khuẩn")
            ExtractCollectionTechs(m, vocab, e);
            (m, _) = ExtractCategories(m, a, vocab, e);
            ExtractPlaces(m, vocab, e);
            e.Occasion = ChatbotKnowledge.Occasions.FirstOrDefault(r => r.Keywords.Any(k => m.Contains(" " + k.Trim() + " ")));
            e.Weather = ChatbotKnowledge.Weathers.FirstOrDefault(r => r.Keywords.Any(k => m.Contains(" " + k.Trim() + " ")));
            return e;
        }

        // ------------------------------------------------------------------ Mã đơn hàng: "#12", "đơn 12", "đơn hàng số 12"
        private static string ExtractOrderId(string m, ChatEntities e)
        {
            var match = Regex.Match(m, @"(?:#\s*|\bdon(?: hang)?(?: so)?\s*#?\s*|\bma don\s*#?\s*)(\d{1,7})\b");
            if (!match.Success) return m;
            e.OrderId = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return Blank(m, match);
        }

        // ------------------------------------------------------------------ Mã khuyến mãi (so với danh sách mã trong database)
        private static string ExtractPromoCode(string m, ChatVocabulary vocab, ChatEntities e)
        {
            foreach (var p in vocab.Promotions)
            {
                var code = " " + p.Code.ToLowerInvariant() + " ";
                var index = m.IndexOf(code, StringComparison.Ordinal);
                if (index < 0) continue;
                e.PromoCode = p.Code;
                return m.Remove(index, code.Length).Insert(index, new string(' ', code.Length));
            }
            // Mã không có trong database nhưng trông giống mã (chữ + số, viết liền, ví dụ "giam50k") -> vẫn lấy để báo "mã không tồn tại"
            var guess = Regex.Match(m, @"\bma\s+([a-z]{3,}\d{1,3}k?)\b");
            if (guess.Success) { e.PromoCode = guess.Groups[1].Value.ToUpperInvariant(); return Blank(m, guess); }
            return m;
        }

        // ------------------------------------------------------------------ Chiều cao, cân nặng
        private static string ExtractBody(string m, ChatEntities e)
        {
            // Cân nặng: "65kg", "65 ký", "65 ki", "75 cân", "nặng 65"
            var w = Regex.Match(m, @"\b(\d{2,3})(?:[.,]\d)?\s*(?:kg|kgs|ky|ki|kilo|can)\b");
            if (!w.Success) w = Regex.Match(m, @"\bnang\s*(\d{2,3})\b");
            if (w.Success)
            {
                var kg = int.Parse(w.Groups[1].Value, CultureInfo.InvariantCulture);
                if (kg is >= 30 and <= 200) { e.WeightKg = kg; m = Blank(m, w); }
            }

            // Chiều cao: "1m70", "1m7", "1,7m", "170cm", "cao 170", hoặc số 140-209 đứng riêng khi đã có cân nặng
            var h = Match.Empty;
            if ((h = Regex.Match(m, @"\b1\s*m\s*(\d{1,2})\b")).Success)
            {
                var d = int.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture);
                e.HeightCm = 100 + (h.Groups[1].Value.Length == 1 ? d * 10 : d);
            }
            else if ((h = Regex.Match(m, @"\b1[.,](\d{1,2})\s*m\b")).Success)
            {
                var d = h.Groups[1].Value;
                e.HeightCm = 100 + int.Parse(d.Length == 1 ? d + "0" : d, CultureInfo.InvariantCulture);
            }
            else if ((h = Regex.Match(m, @"\b(1\d{2})\s*cm\b")).Success || (h = Regex.Match(m, @"\bcao\s*(1\d{2})\b")).Success)
            {
                e.HeightCm = int.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            else if (e.WeightKg != null && (h = Regex.Match(m, @"\b(1[4-9]\d|20\d)\b(?!\s*(?:k|nghin|ngan|tr|trieu|d|dong|vnd)\b)")).Success)
            {
                e.HeightCm = int.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            if (e.HeightCm is < 120 or > 220) e.HeightCm = null;
            if (h.Success && e.HeightCm != null) m = Blank(m, h);
            return m;
        }

        // ------------------------------------------------------------------ Giá tiền
        private const string MoneyPattern =
            @"(\d{1,3}(?:[.,]\d{3})+|\d+(?:[.,]\d+)?)\s*(k|nghin|ngan|tr|trieu|cu|d|dong|vnd)?(?:\s*(\d)\b)?(\s*ruoi)?";

        private static string ExtractPrice(string m, ChatEntities e)
        {
            var found = new List<(Match Match, decimal Value, string Before)>();
            foreach (Match match in Regex.Matches(m, @"\b" + MoneyPattern + @"\b"))
            {
                var before = m[..match.Index];
                var hasUnit = match.Groups[2].Success;
                var hasPriceWord = Regex.IsMatch(before, @"(?:\b(?:duoi|tren|tu|den|toi|khoang|tam|gia|re hon|toi da|khong qua|ngan sach|tien|co|chung|hon)|-)\s*$");
                if (!hasUnit && !hasPriceWord) continue;
                var value = ParseMoney(match);
                if (value is null or < 10_000 or > 100_000_000) continue;
                found.Add((match, value.Value, before));
            }
            if (found.Count == 0) return m;

            if (found.Count >= 2)
            {
                // "từ 200k đến 500k", "200k - 500k", "200 tới 500k"
                var lo = Math.Min(found[0].Value, found[1].Value);
                var hi = Math.Max(found[0].Value, found[1].Value);
                e.MinPrice = lo; e.MaxPrice = hi;
            }
            else
            {
                var (_, value, before) = found[0];
                if (Regex.IsMatch(before, @"\b(duoi|toi da|khong qua|re hon|nho hon|it hon|chi co)\s*$")) e.MaxPrice = value;
                else if (Regex.IsMatch(before, @"\b(tren|hon|lon hon|tu)\s*$")) e.MinPrice = value;
                else { e.MinPrice = RoundThousand(value * 0.75m); e.MaxPrice = RoundThousand(value * 1.2m); } // "tầm 500k" -> 375k - 600k
            }
            foreach (var f in found) m = Blank(m, f.Match);
            return m;
        }

        private static decimal RoundThousand(decimal value) => Math.Round(value / 1000m) * 1000m;

        private static decimal? ParseMoney(Match match)
        {
            var raw = match.Groups[1].Value;
            decimal number;
            if (Regex.IsMatch(raw, @"^\d{1,3}(?:[.,]\d{3})+$")) number = decimal.Parse(Regex.Replace(raw, "[.,]", ""), CultureInfo.InvariantCulture);
            else if (!decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out number)) return null;

            var unit = match.Groups[2].Value;
            decimal result = unit switch
            {
                "k" or "nghin" or "ngan" => number * 1_000,
                "tr" or "trieu" or "cu" => number * 1_000_000,
                "d" or "dong" or "vnd" => number,
                _ => number < 5_000 ? number * 1_000 : number   // "dưới 500" hiểu là 500 nghìn
            };
            if (unit is "tr" or "trieu" or "cu")
            {
                if (match.Groups[3].Success) result += decimal.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) * 100_000; // "1tr5"
                if (match.Groups[4].Success) result += 500_000;                                                                     // "1 triệu rưỡi"
            }
            return result;
        }

        // ------------------------------------------------------------------ Size
        private static string ExtractSizes(string m, string original, ChatEntities e)
        {
            foreach (Match s in Regex.Matches(m, @"\b(?:size|sai|co|so)\s*(xxxl|xxl|xl|xs|s|m|l|[2-4]\d)\b"))
            {
                AddSize(e, s.Groups[1].Value);
                m = Blank(m, s);
            }
            foreach (Match s in Regex.Matches(m, @"\b(xxxl|xxl|xl)\b")) { AddSize(e, s.Groups[1].Value); m = Blank(m, s); }
            // Chữ in hoa đứng riêng trong câu gốc: "còn M không", "lấy L"
            foreach (Match s in Regex.Matches(original, @"(?<![\p{L}\d])(XS|S|M|L)(?![\p{L}\d])")) AddSize(e, s.Groups[1].Value);
            return m;
        }

        private static void AddSize(ChatEntities e, string size)
        {
            var value = size.ToUpperInvariant();
            if (!e.Sizes.Contains(value)) e.Sizes.Add(value);
        }

        // ------------------------------------------------------------------ Màu (theo danh sách màu thật trong database)
        private static (string, string) ExtractColors(string m, string a, bool typedAccents, ChatVocabulary vocab, ChatEntities e)
        {
            var colors = vocab.Colors.Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => (Db: c, Plain: Clean(c), Lower: c.ToLowerInvariant()))
                .ToList();

            // 1. Màu nhiều chữ ("xanh navy", "đỏ đô", "vàng mù tạt"): khớp dạng không dấu
            foreach (var c in colors.Where(c => c.Plain.Contains(' ')).OrderByDescending(c => c.Plain.Length))
            {
                if (!m.Contains(" " + c.Plain + " ")) continue;
                if (!e.Colors.Contains(c.Db)) e.Colors.Add(c.Db);
                if (!e.ColorWords.Contains(c.Plain)) e.ColorWords.Add(c.Plain);
                m = m.Replace(" " + c.Plain + " ", "  ");
                a = a.Replace(" " + c.Lower + " ", "  ");
            }

            // 2. Màu 1 chữ ("đen", "trắng", "xanh"): cần gõ đúng dấu, hoặc có chữ "màu" phía trước ("mau den")
            var words = colors.Select(c => (Plain: c.Plain.Split(' ')[0], Lower: c.Lower.Split(' ')[0]))
                .Concat(new[] { (Plain: "trang", Lower: "trắng") })
                .Distinct();
            foreach (var (plain, lower) in words)
            {
                var accentedHit = typedAccents && a.Contains(" " + lower + " ");
                var withMau = m.Contains(" mau " + plain + " ");
                if (!accentedHit && !withMau) continue;
                foreach (var c in colors.Where(c => c.Plain == plain || c.Plain.StartsWith(plain + " ")))
                    if (!e.Colors.Contains(c.Db)) e.Colors.Add(c.Db);
                if (!e.ColorWords.Contains(plain)) e.ColorWords.Add(plain);
                m = m.Replace(" mau " + plain + " ", "  ");
                a = a.Replace(" " + lower + " ", "  ");
            }
            return (m, a);
        }

        // ------------------------------------------------------------------ Danh mục (tên trong database + tên gọi khác)
        private static (string, string) ExtractCategories(string m, string a, ChatVocabulary vocab, ChatEntities e)
        {
            var phrases = new List<(string Phrase, bool Accented, int CategoryId)>();
            foreach (var c in vocab.Categories)
            {
                var plain = Clean(c.Name);
                // Tên 1 chữ ngắn dễ trùng nghĩa khi bỏ dấu ("Dép"/"đẹp", "Ví"/"vì", "Nón"/"non") -> so dạng có dấu
                if (!plain.Contains(' ') && plain.Length <= 4) phrases.Add((c.Name.ToLowerInvariant(), true, c.Id));
                else phrases.Add((plain, false, c.Id));

                if (ChatbotKnowledge.CategorySynonyms.TryGetValue(c.Name, out var synonyms))
                    foreach (var s in synonyms)
                        phrases.Add((s, TextNormalizer.HasAccents(s), c.Id));
            }

            foreach (var (phrase, accented, id) in phrases.OrderByDescending(p => p.Phrase.Length))
            {
                var key = " " + phrase + " ";
                if (accented ? !a.Contains(key) : !m.Contains(key)) continue;
                if (!e.CategoryIds.Contains(id)) e.CategoryIds.Add(id);
                if (accented) a = a.Replace(key, "  ");
                else m = m.Replace(key, "  ");
            }
            return (m, a);
        }

        // ------------------------------------------------------------------ Bộ sưu tập / công nghệ vải
        private static void ExtractCollectionTechs(string m, ChatVocabulary vocab, ChatEntities e)
        {
            foreach (var t in vocab.CollectionTechs.Where(t => t.Name != "Non-Branded").OrderByDescending(t => t.Name.Length))
            {
                var key = " " + Clean(t.Name) + " ";
                if (!m.Contains(key)) continue;
                e.CollectionTechIds.Add(t.Id);
                e.CollectionTechNames.Add(t.Name);
                m = m.Replace(key, "  ");
            }
        }

        // ------------------------------------------------------------------ Địa danh (quận / thành phố có cửa hàng)
        private static void ExtractPlaces(string m, ChatVocabulary vocab, ChatEntities e)
        {
            var phrases = new Dictionary<string, string>(); // cách nói -> địa danh chuẩn (không dấu)
            foreach (var s in vocab.Stores)
            {
                var parts = s.Location.Split(',').Select(p => Clean(p)).Where(p => p.Length > 0).ToList();
                if (parts.Count < 2) continue;
                foreach (var place in parts.TakeLast(2))
                {
                    phrases.TryAdd(place, place);
                    var core = Regex.Replace(place, @"^(quan|huyen|tp\.?|thanh pho|thi xa|tinh)\s+", "");
                    if (core != place && core.Length >= 4 && !Regex.IsMatch(core, @"^\d+$")) phrases.TryAdd(core, place);
                }
            }
            foreach (var (canonical, synonyms) in ChatbotKnowledge.PlaceSynonyms)
                foreach (var s in synonyms) phrases.TryAdd(s, canonical);

            foreach (var (phrase, canonical) in phrases.OrderByDescending(p => p.Key.Length))
            {
                var key = " " + phrase + " ";
                if (!m.Contains(key)) continue;
                if (!e.Places.Contains(canonical)) e.Places.Add(canonical);
                m = m.Replace(key, "  ");
            }
        }

        // ------------------------------------------------------------------ Hàm phụ
        /// <summary>Bỏ dấu, chữ thường, bỏ ký tự & / , để so khớp: "Balo, Túi & Phụ Kiện Da" -> "balo tui phu kien da"</summary>
        public static string Clean(string text)
            => Regex.Replace(Regex.Replace(TextNormalizer.StripAccents(text.ToLowerInvariant()), @"[&/,()]", " "), @"\s+", " ").Trim();

        /// <summary>Xóa đoạn đã nhận ra (thay bằng khoảng trắng, giữ nguyên độ dài câu)</summary>
        private static string Blank(string text, Match match)
            => text.Remove(match.Index, match.Length).Insert(match.Index, new string(' ', match.Length));
    }
}
