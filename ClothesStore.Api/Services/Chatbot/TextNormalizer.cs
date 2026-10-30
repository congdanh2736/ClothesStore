// [THÊM MỚI] Chatbot AI - chuẩn hóa câu chữ trước khi đưa vào mô hình / tìm thông tin
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClothesStore.Api.Services.Chatbot
{
    public static class TextNormalizer
    {
        /// <summary>Bỏ dấu tiếng Việt: "Áo khoác đen" -> "Ao khoac den".</summary>
        public static string StripAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var decomposed = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Câu có gõ dấu tiếng Việt hay không (để biết có tin được từ ngắn như "đen", "đỏ" không).</summary>
        public static bool HasAccents(string text) => StripAccents(text) != text;

        /// <summary>Chuẩn hóa để đưa vào mô hình: chữ thường, gom khoảng trắng (giống lúc train).</summary>
        public static string ForModel(string text)
            => Regex.Replace((text ?? "").Trim().ToLowerInvariant(), @"\s+", " ");

        // Viết tắt hay gặp khi chat -> từ đầy đủ (đã bỏ dấu)
        private static readonly Dictionary<string, string> Abbreviations = new()
        {
            // (không đổi "hong" vì trùng màu "hồng" khi gõ không dấu)
            ["ko"] = "khong", ["k"] = "khong", ["khum"] = "khong", ["hok"] = "khong", ["kh"] = "khong",
            ["dc"] = "duoc", ["đc"] = "duoc", ["bn"] = "bao nhieu", ["bnhiu"] = "bao nhieu",
            ["mk"] = "minh", ["mik"] = "minh", ["vs"] = "voi", ["j"] = "gi", ["ntn"] = "nhu the nao",
            ["sz"] = "size", ["qan"] = "quan", ["camon"] = "cam on", ["tks"] = "cam on", ["thanks"] = "cam on",
            ["sg"] = "sai gon", ["hcm"] = "ho chi minh", ["hn"] = "ha noi", ["sp"] = "san pham", ["ad"] = "shop"
        };

        /// <summary>
        /// Chuẩn hóa để TÌM THÔNG TIN: chữ thường, bỏ dấu, đổi viết tắt, bỏ ký tự lạ.
        /// Giữ lại # . , - / để đọc mã đơn, giá tiền (500.000), khoảng (200k-500k).
        /// Kết quả có 1 khoảng trắng ở đầu và cuối để tìm theo nguyên từ: Contains(" ao khoac ").
        /// </summary>
        public static string ForMatching(string text)
        {
            var s = StripAccents((text ?? "").ToLowerInvariant());
            s = Regex.Replace(s, @"[^a-z0-9#.,\-/ ]", " ");
            // tách số và chữ dính nhau kiểu "500k", "1tr5" vẫn giữ nguyên để đọc giá; chỉ đổi viết tắt theo nguyên từ
            var words = Regex.Split(s.Trim(), @"\s+");
            for (var i = 0; i < words.Length; i++)
            {
                // "500 k" là giá tiền (500 nghìn), không phải "không"
                if (words[i] == "k" && i > 0 && Regex.IsMatch(words[i - 1], @"^\d")) continue;
                if (Abbreviations.TryGetValue(words[i], out var full)) words[i] = full;
            }
            return " " + string.Join(' ', words) + " ";
        }
    }
}
