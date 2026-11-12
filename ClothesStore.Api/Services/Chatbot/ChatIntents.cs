// [THÊM MỚI] Chatbot AI - tên các intent (ý định) mà mô hình nhận diện
namespace ClothesStore.Api.Services.Chatbot
{
    /* Tên các intent (phải trùng cột Label trong intents.csv) */
    public static class ChatIntents
    {
        public const string ModelName = "IntentModel"; // tên đăng ký mô hình trong Program.cs

        public const string TimSanPham = "tim_san_pham";
        public const string TuVanSize = "tu_van_size";
        public const string TuVanDip = "tu_van_dip";
        public const string TuVanThoiTiet = "tu_van_thoi_tiet";
        public const string PhoiDo = "phoi_do";
        public const string KhuyenMai = "khuyen_mai";
        public const string ConHang = "con_hang";
        public const string CuaHang = "cua_hang";
        public const string ChinhSach = "chinh_sach";
        public const string DonHang = "don_hang";
        public const string ChaoHoi = "chao_hoi";
        public const string CamOn = "cam_on";
        public const string NgoaiPhamVi = "ngoai_pham_vi";

        // Không phải intent của mô hình: dùng khi mô hình không đủ chắc chắn
        public const string KhongHieu = "khong_hieu";

        /// <summary>Tên tiếng Việt của từng intent (hiển thị ở trang admin)</summary>
        public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
        {
            [TimSanPham] = "Tìm sản phẩm",
            [TuVanSize] = "Tư vấn size",
            [TuVanDip] = "Tư vấn theo dịp",
            [TuVanThoiTiet] = "Thời tiết / chất liệu",
            [PhoiDo] = "Phối đồ",
            [KhuyenMai] = "Khuyến mãi",
            [ConHang] = "Còn hàng",
            [CuaHang] = "Cửa hàng",
            [ChinhSach] = "Chính sách",
            [DonHang] = "Đơn hàng",
            [ChaoHoi] = "Chào hỏi",
            [CamOn] = "Cảm ơn / tạm biệt",
            [NgoaiPhamVi] = "Ngoài phạm vi"
        };

        /// <summary>Các intent mô hình học (không gồm khong_hieu)</summary>
        public static IEnumerable<string> All => Labels.Keys;

        public static bool IsValid(string? intent) => intent != null && Labels.ContainsKey(intent);
    }
}
