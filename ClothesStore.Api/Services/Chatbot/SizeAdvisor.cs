// [THÊM MỚI] Chatbot AI - chọn size theo chiều cao / cân nặng dựa trên bảng size (SizeChart) của từng danh mục
using System.Globalization;
using System.Text.RegularExpressions;
using ClothesStore.Api.DTOs.Chatbot;

namespace ClothesStore.Api.Services.Chatbot
{
    /* Kết quả gợi ý size */
    public class SizeRecommendation
    {
        public string SizeLabel { get; set; } = string.Empty;
        public string Measurements { get; set; } = string.Empty;
        public bool FitsWell { get; set; }          // chiều cao và cân nặng đều nằm trong khoảng của size này
        public string? AlternativeSize { get; set; } // size kế bên nên cân nhắc (khi chiều cao và cân nặng lệch nhau)
    }

    /*
     * Bảng size trong database ghi dạng chữ, ví dụ:
     *   "Cao 1m62-1m68 | Nặng 55-62kg | Ngực 96cm | Dài áo 68cm"   (áo)
     *   "Vòng eo 77cm | Vòng mông 95cm | Dài quần 99cm | Nặng 55-60kg" (quần)
     * -> đọc ra khoảng chiều cao (cm) và cân nặng (kg) của từng size, rồi chọn size gần với khách nhất.
     */
    public static class SizeAdvisor
    {
        private record ParsedRow(int Index, ChatSizeRow Row, (int Min, int Max)? Height, (int Min, int Max)? Weight);

        /// <summary>Đọc khoảng chiều cao: "Cao 1m62-1m68" -> (162, 168)</summary>
        public static (int Min, int Max)? ParseHeight(string measurements)
        {
            var m = Regex.Match(TextNormalizer.StripAccents(measurements.ToLowerInvariant()), @"cao\s*1m(\d{2})\s*-\s*1m(\d{2})");
            return m.Success ? (100 + Int(m.Groups[1].Value), 100 + Int(m.Groups[2].Value)) : null;
        }

        /// <summary>Đọc khoảng cân nặng: "Nặng 55-62kg" -> (55, 62)</summary>
        public static (int Min, int Max)? ParseWeight(string measurements)
        {
            var m = Regex.Match(TextNormalizer.StripAccents(measurements.ToLowerInvariant()), @"nang\s*(\d{2,3})\s*-\s*(\d{2,3})\s*kg");
            return m.Success ? (Int(m.Groups[1].Value), Int(m.Groups[2].Value)) : null;
        }

        /// <summary>Bảng size này có dùng được để tư vấn theo chiều cao / cân nặng không (giày, nón... thì không)</summary>
        public static bool CanAdvise(IEnumerable<ChatSizeRow> rows)
            => rows.Any(r => ParseHeight(r.Measurements) != null || ParseWeight(r.Measurements) != null);

        /// <summary>Bảng size có cột chiều cao hay không (quần chỉ có cân nặng)</summary>
        public static bool HasHeight(IEnumerable<ChatSizeRow> rows) => rows.Any(r => ParseHeight(r.Measurements) != null);

        /// <summary>Chọn size. Trả về null nếu bảng size không có thông tin chiều cao / cân nặng.</summary>
        public static SizeRecommendation? Recommend(IReadOnlyList<ChatSizeRow> rows, int? heightCm, int? weightKg)
        {
            if (heightCm == null && weightKg == null) return null;

            var parsed = rows.Select((r, i) => new ParsedRow(i, r, ParseHeight(r.Measurements), ParseWeight(r.Measurements)))
                .Where(p => (heightCm != null && p.Height != null) || (weightKg != null && p.Weight != null))
                .ToList();
            if (parsed.Count == 0) return null;

            /*
             * "Điểm lệch" của từng size = khoảng cách từ số đo của khách tới khoảng của size đó (nằm trong khoảng = 0).
             * Cân nặng quan trọng hơn chiều cao (quyết định áo/quần có chật không) nên nhân 1.5.
             * Điểm bằng nhau thì chọn size LỚN hơn (mặc rộng một chút dễ chịu hơn mặc chật).
             */
            double Penalty(ParsedRow p)
                => Distance(heightCm, p.Height) + 1.5 * Distance(weightKg, p.Weight);

            var best = parsed.OrderBy(Penalty).ThenByDescending(p => p.Index).First();

            var result = new SizeRecommendation
            {
                SizeLabel = best.Row.SizeLabel,
                Measurements = best.Row.Measurements,
                FitsWell = Penalty(best) == 0
            };

            // Chiều cao hợp size này nhưng cân nặng hợp size khác (hoặc ngược lại) -> nhắc khách cân nhắc size kia
            var byHeight = heightCm == null ? null : parsed.FirstOrDefault(p => Distance(heightCm, p.Height) == 0 && p.Height != null);
            var byWeight = weightKg == null ? null : parsed.FirstOrDefault(p => Distance(weightKg, p.Weight) == 0 && p.Weight != null);
            var other = new[] { byHeight, byWeight }.FirstOrDefault(p => p != null && p.Index != best.Index);
            if (other != null) result.AlternativeSize = other.Row.SizeLabel;

            return result;
        }

        private static double Distance(int? value, (int Min, int Max)? range)
        {
            if (value == null || range == null) return 0;
            if (value < range.Value.Min) return range.Value.Min - value.Value;
            if (value > range.Value.Max) return value.Value - range.Value.Max;
            return 0;
        }

        private static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);
    }
}
