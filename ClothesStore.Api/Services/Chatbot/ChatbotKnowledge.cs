// [THÊM MỚI] Chatbot AI - "kiến thức" thời trang do nhóm tự soạn (luật tư vấn, chính sách, câu trả lời mẫu)
namespace ClothesStore.Api.Services.Chatbot
{
    /*
     * Mô hình ML chỉ đoán khách MUỐN GÌ (intent). Nội dung tư vấn lấy từ:
     *   - database (sản phẩm, bảng size, khuyến mãi, cửa hàng, đơn hàng)
     *   - các luật trong file này (dịp nào mặc gì, thời tiết nào chọn vải gì, màu nào phối với màu nào...)
     * Tên danh mục / bộ sưu tập / công nghệ vải ở đây phải TRÙNG tên trong database (Data/SeedData).
     * Từ khóa viết KHÔNG DẤU, chữ thường (vì câu hỏi được bỏ dấu trước khi so khớp).
     */
    public static class ChatbotKnowledge
    {
        /* Một luật tư vấn: khi câu hỏi có 1 trong các Keywords -> gợi ý các danh mục / công nghệ / bộ sưu tập + lời khuyên */
        public record AdviceRule(string Key, string Title, string[] Keywords, string[] Categories,
            string[] CollectionTechs, string Tip);

        // ------------------------------------------------------------------ Tư vấn theo DỊP
        public static readonly AdviceRule[] Occasions =
        {
            new("cong_so", "đi làm / công sở",
                new[] { "di lam", "van phong", "cong so", "phong van", "doi tac", "gap khach", "di hop", "thuc tap", "cong tac", "di lam viec" },
                new[] { "Áo Sơ Mi Tay Dài", "Quần Tây Nam Smart", "Áo Thun Cổ Polo & Henley", "Thắt Lưng" },
                new[] { "The CEO", "Quần Tây Thông Minh", "Sơ Mi Kháng Khuẩn", "Chống Nhăn" },
                "Sơ mi sáng màu (trắng, xanh nhạt) + quần tây tối màu (đen, xanh navy, xám) là công thức an toàn nhất. Sơ vin gọn gàng, thắt lưng và giày cùng tông màu. Đi phỏng vấn thì ưu tiên sơ mi tay dài, tránh họa tiết lớn."),
            new("tiec", "đi tiệc / đám cưới / sự kiện",
                new[] { "dam cuoi", "an cuoi", "tiec", "su kien", "gia dinh nguoi yeu", "ky yeu", "le chua", "tet", "sinh nhat" },
                new[] { "Áo Sơ Mi Tay Dài", "Quần Tây Nam Smart", "Áo Khoác Gile / Da", "Giày" },
                new[] { "The CEO", "Chống Nhăn" },
                "Chọn sơ mi trơn màu sáng hoặc màu trầm, quần tây đứng form, giày da sạch sẽ. Muốn nổi bật hơn có thể khoác thêm gile. Tránh quần đùi, dép khi đi đám cưới."),
            new("di_choi", "đi chơi / hẹn hò / cà phê",
                new[] { "di choi", "hen ho", "di date", "cafe", "ca phe", "xem phim", "hop lop", "di nhau", "concert", "cuoi tuan", "dao pho" },
                new[] { "Áo Thun Cổ Polo & Henley", "Áo Sơ Mi Tay Ngắn", "Quần Jeans", "Quần Dài Lưng Gài", "Giày" },
                new[] { "The Weekend", "Seventy Seven", "The Minimalist" },
                "Polo hoặc sơ mi tay ngắn + quần jeans / kaki là đủ lịch sự mà vẫn thoải mái. Đi hẹn hò nên chọn đồ vừa người, màu trung tính (trắng, be, xanh navy) và chú ý giày sạch."),
            new("di_bien", "đi biển / du lịch hè",
                new[] { "di bien", "tam bien", "nghi mat", "du lich", "vung tau", "nha trang", "phu quoc", "da nang" },
                new[] { "Áo Thun Tank Top", "Áo Sơ Mi Tay Ngắn", "Quần Đùi", "Dép", "Nón" },
                new[] { "Summer Vibe Shorts", "Linen & Cargo Pants", "Vải Mát", "Khô Thoáng" },
                "Ưu tiên vải mát, nhanh khô và màu sáng. Một chiếc sơ mi tay ngắn khoác ngoài tank top + quần short là set đi biển dễ phối, đừng quên nón để che nắng."),
            new("vung_lanh", "đi Đà Lạt / leo núi / cắm trại",
                new[] { "da lat", "sapa", "leo nui", "cam trai", "da ngoai", "trekking", "phuot" },
                new[] { "Áo Khoác Thun", "Áo Khoác Dù", "Quần Jeans", "Quần Dài Lưng Thun", "Giày" },
                new[] { "Áo Khoác Thời Tiết", "Bảo Vệ Thời Tiết", "Co Giãn" },
                "Mặc nhiều lớp: áo thun bên trong + áo khoác thun hoặc áo khoác dù chống gió bên ngoài. Quần co giãn và giày đế bám giúp di chuyển thoải mái."),
            new("the_thao", "tập gym / thể thao",
                new[] { "gym", "tap ta", "chay bo", "da bong", "the thao", "yoga", "tap luyen", "di tap", "cau long", "di boi" },
                new[] { "Áo Thun Tank Top", "Quần Đùi Lưng Thun", "Quần Dài Lưng Thun", "Băng Đeo Thể Thao" },
                new[] { "The Trainer", "Khô Thoáng", "Co Giãn" },
                "Chọn đồ co giãn, thấm hút mồ hôi và nhanh khô. Tank top + quần short lưng thun là thoải mái nhất khi tập."),
            new("o_nha", "mặc ở nhà",
                new[] { "o nha", "mac nha", "di ngu", "do ngu", "o trong nha" },
                new[] { "Áo Thun Cổ Tròn", "Quần Đùi Lưng Thun", "Quần Dài Lưng Thun" },
                new[] { "The Home Body", "Vòng Eo Dễ Chịu" },
                "Ở nhà nên chọn áo thun cotton và quần lưng thun rộng rãi, ưu tiên sự thoải mái."),
            new("di_hoc", "đi học",
                new[] { "di hoc", "den truong", "sinh vien", "hoc sinh" },
                new[] { "Áo Thun Cổ Tròn", "Áo Thun Cổ Polo & Henley", "Quần Jeans", "Balo", "Giày" },
                new[] { "The Beginner", "Bộ Sưu Tập Áo Thun 2026" },
                "Áo thun / polo + quần jeans + balo là bộ đi học năng động, dễ phối và bền."),
        };

        // ------------------------------------------------------------------ Tư vấn theo THỜI TIẾT / NHU CẦU VẢI
        public static readonly AdviceRule[] Weathers =
        {
            new("nong", "trời nóng / mùa hè",
                new[] { "nong", "troi nang", "nang nong", "mua he", "he nay", "oi buc", "mat me", "cho mat", "nang gat", "di nang", "nang to" },
                new[] { "Áo Thun", "Áo Sơ Mi Tay Ngắn", "Quần Đùi" },
                new[] { "Vải Mát", "Thoáng Mát", "Khô Thoáng" },
                "Trời nóng nên chọn vải mỏng, thoáng khí (vải mát, cotton, linen), màu sáng để đỡ hấp nhiệt, form rộng vừa phải."),
            new("mua", "trời mưa",
                new[] { "troi mua", "mua mua", "mua bao", "mua to", "di mua", "bi uot", "chong nuoc", "chong mua" },
                new[] { "Áo Khoác Dù" },
                new[] { "Bảo Vệ Thời Tiết", "Áo Khoác Thời Tiết", "Khô Thoáng" },
                "Trời mưa nên có một chiếc áo khoác dù chống nước nhẹ, gọn để mang theo; quần nhanh khô sẽ dễ chịu hơn quần jeans dày."),
            new("lanh", "trời lạnh / se lạnh",
                new[] { "lanh", "ret", "mua dong", "mua thu", "se lanh", "gio lon", "troi gio", "can gio", "gio lanh", "giu am", "am ap" },
                new[] { "Áo Khoác Thun", "Áo Khoác Jean", "Áo Khoác Dù", "Áo Sơ Mi Tay Dài", "Quần Jeans" },
                new[] { "Áo Khoác Thời Tiết", "Bảo Vệ Thời Tiết" },
                "Trời lạnh nên mặc nhiều lớp: áo thun hoặc sơ mi tay dài bên trong, áo khoác thun / jean / dù bên ngoài để dễ cởi ra khi ấm hơn."),
            new("mo_hoi", "ra nhiều mồ hôi / dễ có mùi",
                new[] { "mo hoi", "tham hut", "hoi ao", "co mui", "khu mui", "kich ung", "khang khuan", "bi hoi" },
                new[] { "Áo Thun", "Áo Sơ Mi" },
                new[] { "Kháng Khuẩn", "Khô Thoáng", "Sơ Mi Kháng Khuẩn" },
                "Nếu hay ra mồ hôi, chọn vải kháng khuẩn (hạn chế mùi) và khô thoáng (thấm hút, nhanh khô), tránh vải dày bí."),
            new("nhan", "ngại ủi / hay bị nhăn",
                new[] { "nhan nheo", "bi nhan", "hay nhan", "it nhan", "khong nhan", "chong nhan", "ui do", "la ui", "ngai ui" },
                new[] { "Áo Sơ Mi", "Quần Tây" },
                new[] { "Chống Nhăn", "Quần Tây Thông Minh" },
                "Chọn sơ mi và quần tây có công nghệ chống nhăn: giặt xong phơi là mặc được, rất hợp đi công tác."),
            new("van_dong", "vận động nhiều / cần co giãn",
                new[] { "van dong", "co gian", "ngoi nhieu", "di xe may", "bung to", "bo sat" },
                new[] { "Quần Jeans", "Quần Dài", "Áo Thun" },
                new[] { "Co Giãn", "Vòng Eo Dễ Chịu", "Jeans Fit Đột Phá" },
                "Ưu tiên chất liệu co giãn và quần có lưng dễ chịu để ngồi lâu, đi xe máy không bị bó."),
        };

        // ------------------------------------------------------------------ Giải thích công nghệ vải
        public static readonly Dictionary<string, string> TechDescriptions = new()
        {
            ["Vải Mát"] = "sợi vải có khả năng tản nhiệt nhanh, chạm vào thấy mát, hợp mặc mùa hè và đi nắng.",
            ["Thoáng Mát"] = "cấu trúc dệt thông thoáng giúp không khí lưu thông, mặc cả ngày không bí.",
            ["Khô Thoáng"] = "thấm hút mồ hôi tốt và khô nhanh, hợp vận động, chơi thể thao.",
            ["Kháng Khuẩn"] = "vải được xử lý kháng khuẩn, hạn chế vi khuẩn gây mùi, giữ áo thơm lâu hơn.",
            ["Bảo Vệ Thời Tiết"] = "chống thấm nước nhẹ và cản gió, hợp trời mưa nhỏ, đi xe máy, đi phượt.",
            ["Co Giãn"] = "vải co giãn theo cử động, mặc ôm vừa mà vẫn thoải mái khi ngồi, cúi, vận động.",
            ["Vòng Eo Dễ Chịu"] = "phần lưng quần thiết kế co giãn / điều chỉnh, không bị cấn bụng khi ngồi lâu.",
            ["Chống Tuột Áo"] = "chi tiết giữ áo sơ vin không bị tuột ra khỏi quần khi vận động.",
            ["Dệt Nổi"] = "bề mặt vải dệt nổi tạo vân, nhìn cao cấp và ít lộ nhăn.",
            ["Chống Nhăn"] = "vải ít nhăn sau khi giặt / gấp, giặt xong phơi là mặc được, hợp đi làm, đi công tác.",
        };

        // ------------------------------------------------------------------ Phối màu
        public static readonly Dictionary<string, string[]> ColorMatches = new()
        {
            ["den"] = new[] { "Trắng", "Xám", "Be", "Xanh Navy" },
            ["trang"] = new[] { "Đen", "Xanh Navy", "Be", "Xanh Rêu", "Xám" },
            ["xanh navy"] = new[] { "Trắng", "Be", "Xám", "Kem" },
            ["be"] = new[] { "Trắng", "Nâu", "Xanh Navy", "Đen" },
            ["kem"] = new[] { "Nâu", "Xanh Navy", "Xanh Rêu", "Đen" },
            ["xam"] = new[] { "Đen", "Trắng", "Xanh Navy", "Hồng Pastel" },
            ["nau"] = new[] { "Be", "Kem", "Trắng", "Xanh Rêu" },
            ["xanh reu"] = new[] { "Be", "Kem", "Trắng", "Đen" },
            ["hong"] = new[] { "Trắng", "Xám", "Xanh Nhạt", "Be" },
            ["do"] = new[] { "Đen", "Be", "Xám", "Trắng" },
            ["xanh"] = new[] { "Trắng", "Be", "Xám", "Kem" },
            ["cam"] = new[] { "Trắng", "Be", "Xanh Navy" },
            ["vang"] = new[] { "Trắng", "Xanh Navy", "Đen" },
        };

        /// <summary>Nhóm danh mục để phối: món phía trên (áo) đi với món phía dưới (quần) và ngược lại.</summary>
        public static readonly string[] TopRoots = { "Áo Thun", "Áo Sơ Mi", "Áo Khoác" };
        public static readonly string[] BottomRoots = { "Quần Jeans", "Quần Dài", "Quần Tây", "Quần Đùi" };

        // ------------------------------------------------------------------ Tên gọi khác của danh mục
        // Viết KHÔNG DẤU; riêng từ ngắn dễ trùng nghĩa thì viết CÓ DẤU (ví dụ "dép" vì "dep" trùng "đẹp",
        // "nón"/"mũ"/"khăn") -> chỉ nhận khi khách gõ đúng dấu.
        public static readonly Dictionary<string, string[]> CategorySynonyms = new()
        {
            ["Áo Thun"] = new[] { "ao phong", "ao thun tron", "ao thun basic", "ao tee", "t-shirt", "tshirt" },
            ["Áo Thun Cổ Tròn"] = new[] { "ao co tron", "thun co tron" },
            ["Áo Thun Cổ Polo & Henley"] = new[] { "ao polo", "polo", "henley", "ao co be" },
            ["Áo Thun Tank Top"] = new[] { "ao ba lo", "tank top", "tanktop", "ao sat nach" },
            ["Áo Sơ Mi"] = new[] { "so mi", "somi", "ao somi" },
            ["Áo Sơ Mi Tay Ngắn"] = new[] { "so mi ngan tay", "so mi tay ngan", "so mi coc tay", "ao so mi ngan tay" },
            ["Áo Sơ Mi Tay Dài"] = new[] { "so mi dai tay", "so mi tay dai", "ao so mi dai tay" },
            ["Áo Khoác Dù"] = new[] { "ao gio", "ao du", "ao khoac gio", "ao chong nang", "ao khoac chong nang", "ao mua", "windbreaker" },
            ["Áo Khoác Thun"] = new[] { "hoodie", "ao hoodie", "ao ni", "ao len", "cardigan", "ao cardigan", "sweater", "ao khoac ni", "ao noi" },
            ["Áo Khoác Jean"] = new[] { "ao khoac bo", "ao jean", "jacket jean", "ao khoac jeans" },
            ["Áo Khoác Kaki & Canvas"] = new[] { "ao khoac kaki", "ao kaki", "ao khoac canvas" },
            ["Áo Khoác Gile / Da"] = new[] { "gile", "ao gile", "ao khoac da", "ao da", "ao vest", "vest" },
            ["Quần Jeans"] = new[] { "quan jean", "quan bo", "quan jeans", "quan denim", "jeans", "jean" },
            ["Quần Tây"] = new[] { "quan au", "quan tay", "quan cong so" },
            ["Quần Dài Lưng Gài"] = new[] { "quan kaki", "quan khaki", "quan chino" },
            ["Quần Dài Lưng Thun"] = new[] { "quan jogger", "jogger", "quan ni", "quan the thao", "quan thun dai" },
            ["Quần Đùi"] = new[] { "quan short", "quan sot", "quan ngan", "quan dui", "short" },
            ["Quần Đùi Jeans"] = new[] { "short jean", "quan short jean", "quan dui bo", "short jeans" },
            ["Giày"] = new[] { "giày", "sneaker", "giay the thao", "giay da", "giay sneaker" },
            ["Dép"] = new[] { "dép", "sandal", "dep quai ngang", "dep le", "dep ke" },
            ["Nón"] = new[] { "nón", "mũ", "non luoi trai", "mu luoi trai", "bucket", "mu bucket", "non ket" },
            ["Túi Đeo"] = new[] { "tui deo cheo", "tui deo", "tui xach", "tui cheo" },
            ["Túi Tote"] = new[] { "tote", "tui tote", "tui vai" },
            ["Ví"] = new[] { "vi da", "vi nam", "cai vi", "bop da", "vi tien" },
            ["Thắt Lưng"] = new[] { "that lung", "day nit", "day lung" },
            ["Tất Vớ"] = new[] { "tat vo", "doi tat", "doi vo", "vo nam", "tat nam", "tat co" },
            ["Quần Lót"] = new[] { "quan lot", "quan sip", "boxer", "do lot", "brief" },
            ["Balo"] = new[] { "balo", "ba lo", "cap sach" },
            ["Nước Hoa"] = new[] { "nuoc hoa", "perfume" },
            ["Găng Tay"] = new[] { "gang tay" },
            ["Khăn"] = new[] { "khăn", "khan quang", "khan choang" },
        };

        // ------------------------------------------------------------------ Tên gọi khác của địa danh (không dấu)
        public static readonly Dictionary<string, string[]> PlaceSynonyms = new()
        {
            ["tp. ho chi minh"] = new[] { "sai gon", "ho chi minh", "tphcm", "tp hcm", "thanh pho ho chi minh" },
            ["ha noi"] = new[] { "ha noi", "thu do" },
            ["quan 1"] = new[] { "q1", "q.1", "quan 1" }, ["quan 3"] = new[] { "q3", "q.3", "quan 3" },
            ["quan 5"] = new[] { "q5", "q.5", "quan 5" }, ["quan 7"] = new[] { "q7", "q.7", "quan 7" },
            ["quan 10"] = new[] { "q10", "q.10", "quan 10" },
        };

        // ------------------------------------------------------------------ Chính sách (theo đúng luật trong backend)
        public static string ReturnPolicy =>
            "Chính sách trả hàng của shop:\n" +
            "• Gửi yêu cầu trả hàng trong vòng 30 ngày kể từ ngày đặt, áp dụng cho đơn đã giao thành công.\n" +
            "• Vào Tài khoản → Đổi / trả hàng → Tạo yêu cầu, chọn sản phẩm, số lượng, lý do và tình trạng hàng.\n" +
            "• Shop duyệt yêu cầu, nhận lại hàng rồi hoàn tiền (phần giảm giá của mã khuyến mãi được chia đều cho từng món).\n" +
            "• Điểm thưởng tương ứng với phần hàng trả sẽ bị trừ lại.\n" +
            "• Muốn đổi size / màu: bạn trả hàng rồi đặt đơn mới nhé.";

        public static string PaymentPolicy =>
            "Shop nhận các hình thức thanh toán:\n" +
            "• Tiền mặt khi nhận hàng (COD)\n• Ví MoMo\n• VNPay\n• Chuyển khoản ngân hàng\n" +
            "Mua trực tiếp tại cửa hàng có thể trả tiền mặt tại quầy.";

        public static string ShippingPolicy =>
            "Về giao hàng:\n" +
            "• Shop MIỄN PHÍ vận chuyển cho mọi đơn hàng.\n" +
            "• Bạn có thể chọn giao tận nơi hoặc nhận tại cửa hàng khi đặt hàng.\n" +
            "• Thời gian giao tùy đơn vị vận chuyển; khi đơn được giao đi bạn sẽ có mã vận đơn trong mục Đơn hàng của tôi.";

        public static string ReviewPolicy =>
            "Bạn đánh giá được sản phẩm sau khi đơn hàng giao thành công, mỗi sản phẩm 1 lần (1–5 sao, nhận xét, tối đa 3 ảnh) " +
            "và có thể sửa / xóa đánh giá trong Tài khoản → Đánh giá của tôi.";

        public static string AccountPolicy =>
            "Bạn bấm Đăng nhập → Đăng ký ngay, nhập họ tên, email và mật khẩu (ít nhất 6 ký tự, có chữ hoa, chữ thường và số). " +
            "Tài khoản mới tự có giỏ hàng và bắt đầu từ hạng thành viên thấp nhất. Nếu quên mật khẩu, bạn vui lòng liên hệ cửa hàng để được hỗ trợ.";

        // ------------------------------------------------------------------ Câu mẫu
        public static readonly string[] Greetings =
        {
            "Chào bạn! Mình là trợ lý tư vấn của AURA. Mình có thể giúp bạn tìm sản phẩm, chọn size theo chiều cao cân nặng, gợi ý cách phối đồ, xem mã khuyến mãi và tra cứu đơn hàng.",
            "Xin chào! Bạn cần mình tư vấn gì hôm nay? Bạn có thể hỏi kiểu \"cao 1m70 nặng 65kg mặc size gì\" hoặc \"đi đám cưới nên mặc gì\".",
        };

        public static readonly string[] Thanks =
        {
            "Không có gì đâu! Cần thêm gì bạn cứ nhắn mình nhé.",
            "Rất vui được giúp bạn. Chúc bạn mua sắm vui vẻ!",
        };

        public const string OutOfScope =
            "Xin lỗi, mình chỉ hỗ trợ các câu hỏi về sản phẩm, size, phối đồ, khuyến mãi, cửa hàng và đơn hàng của AURA thôi. Bạn thử hỏi mình về quần áo nhé!";

        public const string NotUnderstood =
            "Mình chưa hiểu rõ ý bạn. Bạn có thể hỏi cụ thể hơn, ví dụ: \"tìm áo khoác đen dưới 500k\", \"cao 1m70 nặng 65kg mặc size gì\" hoặc \"áo sơ mi trắng phối với quần gì\".";

        public static readonly string[] DefaultSuggestions =
        {
            "Cao 1m70 nặng 65kg mặc size gì?", "Đi làm văn phòng nên mặc gì?", "Có mã giảm giá nào không?", "Tìm áo khoác dưới 500k"
        };

        /* Dự phòng khi chưa có file mô hình: đoán intent bằng từ khóa đơn giản (không dấu) */
        public static readonly (string Intent, string[] Keywords)[] KeywordFallback =
        {
            (ChatIntents.TuVanSize, new[] { "size", "kg", "can nang", "bang size", "mac vua" }),
            (ChatIntents.KhuyenMai, new[] { "ma giam", "khuyen mai", "voucher", "sale", "giam gia" }),
            (ChatIntents.DonHang, new[] { "don hang", "don cua", "huy don", "van don", "giao chua" }),
            (ChatIntents.CuaHang, new[] { "cua hang", "chi nhanh", "dia chi", "mo cua" }),
            (ChatIntents.ChinhSach, new[] { "doi tra", "tra hang", "hoan tien", "tich diem", "thanh toan", "phi ship", "van chuyen" }),
            (ChatIntents.PhoiDo, new[] { "phoi", "mix", "ket hop", "di voi" }),
            (ChatIntents.ConHang, new[] { "con hang", "het hang", "con size", "con mau" }),
            (ChatIntents.TuVanDip, new[] { "nen mac gi", "mac gi", "outfit" }),
            (ChatIntents.CamOn, new[] { "cam on", "bye", "tam biet" }),
            (ChatIntents.ChaoHoi, new[] { "chao", "hello", "hi " }),
            (ChatIntents.TimSanPham, new[] { "tim", "mua", "co ban", "xem" }),
        };
    }
}
