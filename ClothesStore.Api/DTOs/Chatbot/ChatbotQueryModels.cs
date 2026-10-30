// [THÊM MỚI] Chatbot AI - các kiểu dữ liệu nhỏ dùng giữa ChatbotService và ChatbotRepository
namespace ClothesStore.Api.DTOs.Chatbot
{
    /*
     * "Từ điển" của shop lấy từ database: tên danh mục, màu, công nghệ vải, cửa hàng, mã khuyến mãi...
     * Bot dùng để nhận ra các từ này trong câu hỏi. Được lưu tạm (cache) vài phút cho nhanh.
     */
    public class ChatVocabulary
    {
        public List<ChatCategoryInfo> Categories { get; set; } = new();
        public List<string> Colors { get; set; } = new();
        public List<ChatTagInfo> CollectionTechs { get; set; } = new();
        public List<ChatStoreInfo> Stores { get; set; } = new();
        public List<ChatPromotionInfo> Promotions { get; set; } = new();
        public List<ChatTierInfo> Tiers { get; set; } = new();
    }

    public class ChatCategoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }

    public class ChatTagInfo
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "Collection" | "Technology"
        public string Name { get; set; } = string.Empty;
    }

    public class ChatStoreInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class ChatPromotionInfo
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ChatTierInfo
    {
        public string TierName { get; set; } = string.Empty;
        public int MinPoints { get; set; }
    }

    /* Điều kiện tìm sản phẩm */
    public class ChatProductQuery
    {
        public List<int> CategoryIds { get; set; } = new();     // đã gồm cả danh mục con
        public List<string> Colors { get; set; } = new();       // tên màu đúng như trong database
        public List<string> Sizes { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<int> CollectionTechIds { get; set; } = new();       // BẮT BUỘC thuộc 1 trong các bộ sưu tập / công nghệ này
        public List<int> PreferCollectionTechIds { get; set; } = new(); // ƯU TIÊN xếp lên trước (không bắt buộc)
        public List<int> StoreIds { get; set; } = new();        // còn hàng ở các cửa hàng này
        public bool InStockOnly { get; set; }
        public int Take { get; set; } = 6;
    }

    /* Tồn kho của 1 biến thể tại 1 cửa hàng */
    public class ChatStockRow
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    /* Tóm tắt đơn hàng để bot trả lời */
    public class ChatOrderInfo
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string? ShipmentStatus { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
    }

    /* 1 dòng bảng size */
    public class ChatSizeRow
    {
        public string SizeLabel { get; set; } = string.Empty;
        public string Measurements { get; set; } = string.Empty;
    }
}
