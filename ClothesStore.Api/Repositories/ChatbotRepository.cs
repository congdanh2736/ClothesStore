// [THÊM MỚI] Chatbot AI - truy vấn database cho bot
using System.Linq.Expressions;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Chatbot;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    /*
     * Các truy vấn ở đây chỉ để ĐỌC nên đều dùng AsNoTracking và Select (chỉ lấy đúng cột cần),
     * không Include cả bảng -> nhanh, không bị lỗi timeout như các truy vấn GetAll.
     */
    public class ChatbotRepository : IChatbotRepository
    {
        private readonly ApplicationDbContext _context;
        public ChatbotRepository(ApplicationDbContext context) => _context = context;

        public async Task<ChatVocabulary> GetVocabularyAsync()
        {
            return new ChatVocabulary
            {
                Categories = await _context.Categories.AsNoTracking()
                    .Select(c => new ChatCategoryInfo { Id = c.Id, Name = c.Name ?? "", ParentId = c.ParentCategoryId })
                    .ToListAsync(),
                Colors = await _context.ProductVariants.AsNoTracking()
                    .Where(v => v.Color != null && v.Color != "")
                    .Select(v => v.Color!).Distinct().ToListAsync(),
                CollectionTechs = await _context.CollectionTechs.AsNoTracking()
                    .Select(t => new ChatTagInfo { Id = t.Id, Type = t.Type ?? "", Name = t.Name ?? "" })
                    .ToListAsync(),
                Stores = await _context.Stores.AsNoTracking()
                    .Select(s => new ChatStoreInfo { Id = s.Id, Name = s.Name ?? "", Location = s.Location ?? "" })
                    .ToListAsync(),
                Promotions = await _context.Promotions.AsNoTracking()
                    .Select(p => new ChatPromotionInfo { Code = p.Code, DiscountValue = p.DiscountValue, StartDate = p.StartDate, EndDate = p.EndDate })
                    .ToListAsync(),
                Tiers = await _context.MembershipTiers.AsNoTracking()
                    .OrderBy(t => t.MinPoints)
                    .Select(t => new ChatTierInfo { TierName = t.TierName ?? "", MinPoints = t.MinPoints })
                    .ToListAsync()
            };
        }

        public async Task<(List<ChatProductDto> Items, int Total)> SearchProductsAsync(ChatProductQuery q)
        {
            // Điều kiện trên BIẾN THỂ (màu, size, giá, còn hàng) - dùng chung cho lọc sản phẩm và tính giá thấp nhất
            var variantFilter = BuildVariantFilter(q);

            var products = _context.Products.AsNoTracking().AsQueryable();
            if (q.CategoryIds.Count > 0)
                products = products.Where(p => q.CategoryIds.Contains(p.CategoryId));
            if (q.CollectionTechIds.Count > 0)
                products = products.Where(p => p.ProductCollections.Any(pc => q.CollectionTechIds.Contains(pc.CollectionId)));
            products = products.Where(p => p.ProductVariants.AsQueryable().Any(variantFilter));

            var total = await products.CountAsync();

            var preferIds = q.PreferCollectionTechIds;
            var items = await products
                // 1. sản phẩm thuộc bộ sưu tập / công nghệ được ưu tiên lên trước
                .OrderByDescending(p => preferIds.Count > 0 && p.ProductCollections.Any(pc => preferIds.Contains(pc.CollectionId)))
                // 2. bán chạy (tổng số lượng đã bán)
                .ThenByDescending(p => p.ProductVariants.SelectMany(v => v.OrderItems).Sum(oi => (int?)oi.Quantity) ?? 0)
                // 3. mới nhất
                .ThenByDescending(p => p.Id)
                .Take(q.Take)
                .Select(p => new ChatProductDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name ?? "" : "",
                    ImageUrl = p.ProductImages.OrderBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault(),
                    MinPrice = p.ProductVariants.AsQueryable().Where(variantFilter).Min(v => (decimal?)v.Price) ?? 0,
                    Colors = p.ProductVariants.Where(v => v.Color != null).Select(v => v.Color!).Distinct().Take(5).ToList(),
                    InStock = p.ProductVariants.Any(v => v.StoreStocks.Any(s => s.Quantity > 0))
                })
                .ToListAsync();

            return (items, total);
        }

        /* Điều kiện lọc biến thể. Viết thành Expression để EF Core dịch được sang SQL. */
        private static Expression<Func<ProductVariant, bool>> BuildVariantFilter(ChatProductQuery q)
        {
            var colors = q.Colors;
            var sizes = q.Sizes;
            var storeIds = q.StoreIds;
            var hasColors = colors.Count > 0;
            var hasSizes = sizes.Count > 0;
            var hasStores = storeIds.Count > 0;
            var needStock = q.InStockOnly || hasStores;
            var min = q.MinPrice;
            var max = q.MaxPrice;

            return v => (!hasColors || colors.Contains(v.Color!))
                && (!hasSizes || sizes.Contains(v.Size!))
                && (min == null || v.Price >= min)
                && (max == null || v.Price <= max)
                && (!needStock || v.StoreStocks.Any(s => s.Quantity > 0 && (!hasStores || storeIds.Contains(s.StoreId))));
        }

        public async Task<ChatProductDto?> GetProductAsync(int productId)
            => await _context.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => new ChatProductDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name ?? "" : "",
                    ImageUrl = p.ProductImages.OrderBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault(),
                    MinPrice = p.ProductVariants.Min(v => (decimal?)v.Price) ?? 0,
                    Colors = p.ProductVariants.Where(v => v.Color != null).Select(v => v.Color!).Distinct().Take(5).ToList(),
                    InStock = p.ProductVariants.Any(v => v.StoreStocks.Any(s => s.Quantity > 0))
                })
                .FirstOrDefaultAsync();

        public async Task<List<ChatStockRow>> GetProductStockAsync(int productId, List<string> colors, List<string> sizes, List<int> storeIds)
        {
            var hasColors = colors.Count > 0;
            var hasSizes = sizes.Count > 0;
            var hasStores = storeIds.Count > 0;
            return await _context.StoreStocks.AsNoTracking()
                .Where(s => s.Quantity > 0
                    && s.ProductVariant!.ProductId == productId
                    && (!hasColors || colors.Contains(s.ProductVariant.Color!))
                    && (!hasSizes || sizes.Contains(s.ProductVariant.Size!))
                    && (!hasStores || storeIds.Contains(s.StoreId)))
                .Select(s => new ChatStockRow
                {
                    StoreId = s.StoreId,
                    StoreName = s.Store != null ? s.Store.Name ?? "" : "",
                    Location = s.Store != null ? s.Store.Location ?? "" : "",
                    Color = s.ProductVariant!.Color ?? "",
                    Size = s.ProductVariant.Size ?? "",
                    Quantity = s.Quantity
                })
                .ToListAsync();
        }

        public async Task<List<ChatSizeRow>> GetSizeChartAsync(int categoryId)
            => await _context.SizeCharts.AsNoTracking()
                .Where(s => s.CategoryId == categoryId)
                .OrderBy(s => s.Id)
                .Select(s => new ChatSizeRow { SizeLabel = s.SizeLabel, Measurements = s.Measurements })
                .ToListAsync();

        public async Task<int?> GetCustomerIdByUserIdAsync(string applicationUserId)
            => await _context.Customers.AsNoTracking()
                .Where(c => c.ApplicationUserId == applicationUserId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

        public async Task<List<ChatOrderInfo>> GetOrdersAsync(int customerId, int? orderId, int take)
            => await _context.Orders.AsNoTracking()
                .Where(o => o.CustomerId == customerId && (orderId == null || o.Id == orderId))
                .OrderByDescending(o => o.OrderDate)
                .Take(take)
                .Select(o => new ChatOrderInfo
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.OrderItems.Sum(i => (int?)i.Quantity) ?? 0,
                    ShipmentStatus = o.Shipment != null ? o.Shipment.Status : null,
                    Carrier = o.Shipment != null ? o.Shipment.Carrier : null,
                    TrackingNumber = o.Shipment != null ? o.Shipment.TrackingNumber : null
                })
                .ToListAsync();
    }
}
