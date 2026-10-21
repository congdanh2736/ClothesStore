// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: tự thêm DỮ LIỆU MẪU vào database khi chạy Update-Database (và khi chạy ứng dụng lần đầu).
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClothesStore.Api.Data;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Common
{
    /*
     * ============================================================================
     *  SampleDataSeeder - THÊM DỮ LIỆU MẪU (chỉ thêm 1 lần)
     * ============================================================================
     *
     *  DỮ LIỆU Ở ĐÂU?
     *    Thư mục Data/SeedData: mỗi bảng 1 file JSON (Products.json, Orders.json, ...).
     *    Mỗi dòng trong file = 1 dòng trong bảng, tên thuộc tính giống tên trong Models.
     *    Các file này được "nhúng" vào file .dll khi build (xem mục EmbeddedResource trong
     *    ClothesStore.Api.csproj) nên chạy ở đâu cũng đọc được, không lo sai đường dẫn.
     *
     *  KHI NÀO CHẠY?
     *    1. Khi gõ  Update-Database  (Package Manager Console) hoặc  dotnet ef database update
     *       -> EF Core tự gọi hàm Seed() nhờ UseSeeding(...) khai báo trong Program.cs.
     *    2. Khi chạy ứng dụng: Program.cs gọi Seed() sau RoleSeeder, AdminSeeder... (phòng khi quên Update-Database).
     *
     *  CHẠY NHIỀU LẦN CÓ BỊ TRÙNG KHÔNG?
     *    Không. Nếu database đã có dữ liệu mẫu (còn tài khoản khachhang...@clothesstore.vn /
     *    nhanvien...@clothesstore.vn hoặc còn cửa hàng "Kho Online ClothesStore") thì hàm Seed()
     *    bỏ qua, không thêm gì. Muốn thêm lại từ đầu: Drop-Database rồi Update-Database.
     *
     *  ID TRONG FILE JSON
     *    Chỉ là "Id tạm" để các bảng chỉ tới nhau (ví dụ OrderItems.json có orderId = 5).
     *    Khi thêm vào database, SQL Server tự cấp Id mới; seeder ghi nhớ "Id tạm -> Id mới"
     *    rồi sửa lại các khóa ngoại cho đúng. Vì vậy database đang có sẵn dữ liệu cũng không bị đụng Id.
     *
     *  NGÀY GIỜ
     *    Dữ liệu được tạo quanh ngày "anchorDate" ghi trong SeedInfo.json (90 ngày trước ngày đó).
     *    Lúc thêm vào database, mọi ngày giờ được dời đi cùng một số ngày để ngày cuối trùng với
     *    HÔM NAY -> các biểu đồ "7 ngày qua", "30 ngày qua" luôn có số liệu, khuyến mãi luôn có
     *    mã đã hết hạn / đang chạy / sắp diễn ra.
     *
     *  TÀI KHOẢN MẪU
     *    Khách hàng : khachhang001@clothesstore.vn ... khachhang100@clothesstore.vn  /  Khach@123
     *    Nhân viên  : nhanvien001@clothesstore.vn  ... nhanvien100@clothesstore.vn   /  NhanVien@123
     *    (tài khoản Admin vẫn do AdminSeeder tạo như cũ)
     * ============================================================================
     */
    public static class SampleDataSeeder
    {
        public const string CustomerPassword = "Khach@123";
        public const string EmployeePassword = "NhanVien@123";

        // Dấu hiệu "database đã có dữ liệu mẫu" (email so sánh ở dạng viết HOA = cột NormalizedEmail)
        private const string SampleEmailDomain = "@CLOTHESSTORE.VN";
        private const string CentralStoreName = "Kho Online ClothesStore";

        // Thêm mỗi lần 2000 dòng rồi lưu, để EF không phải giữ cả trăm nghìn dòng trong bộ nhớ cùng lúc
        private const int ChunkSize = 2000;

        private const string SavepointName = "SampleData";

        // Đọc JSON: tên thuộc tính kiểu camelCase (orderId), không phân biệt hoa thường
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Trong lý do tích/trừ điểm có chỗ đánh dấu {order:12} hoặc {return:3} = Id tạm của đơn / yêu cầu trả hàng
        private static readonly Regex IdToken = new(@"\{(order|return):(\d+)\}");

        /// <summary>
        /// Bản dùng cho UseAsyncSeeding (EF gọi khi chạy MigrateAsync / EnsureCreatedAsync).
        /// Dữ liệu mẫu chỉ thêm 1 lần nên dùng chung phần xử lý của hàm Seed() cho đơn giản.
        /// </summary>
        public static Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Seed(context);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Thêm dữ liệu mẫu nếu database chưa có.
        /// </summary>
        /// <param name="context">DbContext đang dùng</param>
        /// <param name="log">Hàm in thông báo (bỏ trống = in ra Console)</param>
        public static void Seed(ApplicationDbContext context, Action<string>? log = null)
        {
            log ??= Console.WriteLine;

            // Database chưa cập nhật hết migration (ví dụ vừa chạy "Update-Database 0" để xóa bảng)
            // -> chưa đủ bảng để thêm dữ liệu, bỏ qua
            if (context.Database.GetPendingMigrations().Any())
            {
                log("[Dữ liệu mẫu] Bỏ qua: database còn migration chưa cập nhật.");
                return;
            }

            if (HasSampleData(context))
            {
                log("[Dữ liệu mẫu] Database đã có dữ liệu mẫu nên không thêm lại.");
                return;
            }

            log("[Dữ liệu mẫu] Đang thêm dữ liệu mẫu vào database (chỉ chạy lần đầu, mất khoảng 1 phút)...");
            var watch = Stopwatch.StartNew();

            /*
             * Chạy trong 1 transaction: lỗi giữa chừng thì hủy hết, database không bị dữ liệu dở dang.
             * - Update-Database: EF đã mở sẵn transaction cho migration -> dùng "điểm lưu" (savepoint)
             *   để nếu lỗi chỉ hủy phần dữ liệu mẫu, migration vẫn được giữ.
             * - Chạy ứng dụng: chưa có transaction -> tự mở transaction mới.
             */
            var currentTransaction = context.Database.CurrentTransaction;
            using var ownTransaction = currentTransaction is null ? context.Database.BeginTransaction() : null;
            currentTransaction?.CreateSavepoint(SavepointName);

            // Tắt tự dò thay đổi của EF cho nhanh (ta chỉ thêm mới, không sửa)
            var autoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                var summary = Import(context);
                ownTransaction?.Commit();
                log($"[Dữ liệu mẫu] Xong ({watch.Elapsed.TotalSeconds:0} giây): {summary}.");
            }
            catch (Exception ex)
            {
                if (ownTransaction is not null)
                    ownTransaction.Rollback();
                else
                    currentTransaction!.RollbackToSavepoint(SavepointName);

                log($"[Dữ liệu mẫu] LỖI, không thêm được dữ liệu mẫu (database giữ nguyên như trước): {ex.GetBaseException().Message}");
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
                context.ChangeTracker.Clear();
            }
        }

        // ==================================================================================
        //  ĐỌC TỪNG FILE VÀ THÊM VÀO DATABASE (bảng cha trước, bảng con sau)
        // ==================================================================================
        private static string Import(ApplicationDbContext context)
        {
            var info = ReadFile<SeedInfo>("SeedInfo.json");
            var days = DaysToShift(info, DateTime.Now, DateTime.UtcNow); // số ngày cần dời cho khớp hôm nay

            // ---------- 0. Vai trò (Role) và hạng thành viên: cần có trước ----------
            // (khi chạy Update-Database, RoleSeeder / MembershipTierSeeder trong Program.cs chưa chạy)
            var roleIds = EnsureRoles(context);
            var tiers = EnsureTiers(context);

            // ---------- 1. DANH MỤC: thêm danh mục cha trước, rồi mới tới danh mục con ----------
            var categoryIds = new Dictionary<int, int>();
            var waiting = ReadList<Category>("Categories.json");
            var categoryCount = waiting.Count;
            while (waiting.Count > 0)
            {
                // những danh mục mà danh mục cha đã có trong database (hoặc không có cha)
                var ready = waiting
                    .Where(c => c.ParentCategoryId is null || categoryIds.ContainsKey(c.ParentCategoryId.Value))
                    .ToList();
                if (ready.Count == 0)
                    throw new InvalidOperationException("Categories.json có danh mục cha không tồn tại.");

                Insert(context, ready, c => c.Id, (c, id) => c.Id = id, categoryIds,
                    c => c.ParentCategoryId = NewId(categoryIds, c.ParentCategoryId));
                waiting = waiting.Except(ready).ToList();
            }

            // ---------- 2. BỘ SƯU TẬP / CÔNG NGHỆ, BẢNG SIZE ----------
            var collectionIds = new Dictionary<int, int>();
            Insert(context, ReadList<CollectionTech>("CollectionTechs.json"), x => x.Id, (x, id) => x.Id = id, collectionIds);

            Insert(context, ReadList<SizeChart>("SizeCharts.json"), x => x.Id, (x, id) => x.Id = id, null,
                x => x.CategoryId = categoryIds[x.CategoryId]);

            // ---------- 3. SẢN PHẨM, BIẾN THỂ (màu / size / giá), ẢNH, LIÊN KẾT BỘ SƯU TẬP ----------
            var productIds = new Dictionary<int, int>();
            var products = ReadList<Product>("Products.json");
            Insert(context, products, x => x.Id, (x, id) => x.Id = id, productIds,
                x => x.CategoryId = categoryIds[x.CategoryId]);

            var variantIds = new Dictionary<int, int>();
            Insert(context, ReadList<ProductVariant>("ProductVariants.json"), x => x.VariantId, (x, id) => x.VariantId = id, variantIds,
                x => x.ProductId = productIds[x.ProductId]);

            Insert(context, ReadList<ProductImage>("ProductImages.json"), x => x.ImageId, (x, id) => x.ImageId = id, null,
                x => x.ProductId = productIds[x.ProductId]);

            InsertRows(context, ReadList<ProductCollection>("ProductCollections.json"), x =>
            {
                x.ProductId = productIds[x.ProductId];
                x.CollectionId = collectionIds[x.CollectionId];
            });

            // ---------- 4. CỬA HÀNG + TỒN KHO ----------
            var storeIds = new Dictionary<int, int>();
            Insert(context, ReadList<Store>("Stores.json"), x => x.Id, (x, id) => x.Id = id, storeIds);

            Insert(context, ReadList<StoreStock>("StoreStocks.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.StoreId = storeIds[x.StoreId];
                x.VariantId = variantIds[x.VariantId];
            });

            // ---------- 5. TÀI KHOẢN ĐĂNG NHẬP, NHÂN VIÊN, KHÁCH HÀNG ----------
            var accounts = ReadList<SeedAccount>("Accounts.json");
            InsertAccounts(context, accounts, roleIds);

            var employees = ReadList<Employee>("Employees.json");
            Insert(context, employees, x => x.Id, (x, id) => x.Id = id, null,
                x => x.StoreId = NewId(storeIds, x.StoreId));

            // Hạng thành viên của khách = hạng cao nhất mà tổng điểm (cộng các dòng điểm thưởng) đạt được
            var loyalty = ReadList<LoyaltyTransaction>("LoyaltyTransactions.json");
            var pointsOf = loyalty
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.PointsChange));

            var customerIds = new Dictionary<int, int>();
            var customers = ReadList<Customer>("Customers.json");
            Insert(context, customers, x => x.Id, (x, id) => x.Id = id, customerIds,
                x => x.MembershipTierId = TierFor(tiers, pointsOf.GetValueOrDefault(x.Id))); // x.Id lúc này vẫn là Id tạm

            var addressIds = new Dictionary<int, int>();
            Insert(context, ReadList<Address>("Addresses.json"), x => x.Id, (x, id) => x.Id = id, addressIds,
                x => x.CustomerId = NewId(customerIds, x.CustomerId));

            // ---------- 6. GIỎ HÀNG, YÊU THÍCH ----------
            var cartIds = new Dictionary<int, int>();
            Insert(context, ReadList<Cart>("Carts.json"), x => x.Id, (x, id) => x.Id = id, cartIds,
                x => x.CustomerId = customerIds[x.CustomerId]);

            Insert(context, ReadList<CartItem>("CartItems.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.CartId = cartIds[x.CartId];
                x.VariantId = variantIds[x.VariantId];
            });

            Insert(context, ReadList<Wishlist>("Wishlists.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.CustomerId = customerIds[x.CustomerId];
                x.ProductId = productIds[x.ProductId];
                x.CreatedAt = x.CreatedAt.AddDays(days);
            });

            // ---------- 7. KHUYẾN MÃI ----------
            // Mã khuyến mãi không được trùng: nếu database đã có mã giống (do nhóm tự tạo) thì thêm đuôi -2, -3...
            var usedCodes = new HashSet<string>(context.Promotions.Select(p => p.Code).ToList(), StringComparer.OrdinalIgnoreCase);
            var promotionIds = new Dictionary<int, int>();
            Insert(context, ReadList<Promotion>("Promotions.json"), x => x.Id, (x, id) => x.Id = id, promotionIds, x =>
            {
                x.Code = UniqueCode(x.Code, usedCodes);
                x.StartDate = x.StartDate.AddDays(days);
                x.EndDate = x.EndDate.AddDays(days);
            });

            // ---------- 8. ĐƠN HÀNG, CHI TIẾT ĐƠN, THANH TOÁN, VẬN CHUYỂN ----------
            var orderIds = new Dictionary<int, int>();
            var orders = ReadList<Order>("Orders.json");
            Insert(context, orders, x => x.Id, (x, id) => x.Id = id, orderIds, x =>
            {
                x.CustomerId = customerIds[x.CustomerId];
                x.AddressId = addressIds[x.AddressId];
                x.PromotionId = NewId(promotionIds, x.PromotionId);
                x.StoreId = NewId(storeIds, x.StoreId);
                x.OrderDate = x.OrderDate.AddDays(days);
            });

            var orderItemIds = new Dictionary<int, int>();
            Insert(context, ReadList<OrderItem>("OrderItems.json"), x => x.Id, (x, id) => x.Id = id, orderItemIds, x =>
            {
                x.OrderId = orderIds[x.OrderId];
                x.VariantId = variantIds[x.VariantId];
            });

            Insert(context, ReadList<PaymentTransaction>("PaymentTransactions.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.OrderId = orderIds[x.OrderId];
                x.PaidAt = x.PaidAt?.AddDays(days);
            });

            Insert(context, ReadList<Shipment>("Shipments.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.OrderId = orderIds[x.OrderId];
                x.ShippedDate = x.ShippedDate?.AddDays(days);
                x.DeliveredDate = x.DeliveredDate?.AddDays(days);
            });

            // ---------- 9. ĐỔI / TRẢ HÀNG ----------
            var returnIds = new Dictionary<int, int>();
            Insert(context, ReadList<ReturnRequest>("ReturnRequests.json"), x => x.Id, (x, id) => x.Id = id, returnIds, x =>
            {
                x.OrderId = orderIds[x.OrderId];
                x.RequestDate = x.RequestDate.AddDays(days);
            });

            Insert(context, ReadList<ReturnItem>("ReturnItems.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.ReturnRequestId = returnIds[x.ReturnRequestId];
                x.OrderItemId = orderItemIds[x.OrderItemId];
            });

            // ---------- 10. ĐÁNH GIÁ + ẢNH ĐÁNH GIÁ ----------
            var reviewIds = new Dictionary<int, int>();
            Insert(context, ReadList<Review>("Reviews.json"), x => x.Id, (x, id) => x.Id = id, reviewIds, x =>
            {
                x.CustomerId = customerIds[x.CustomerId];
                x.ProductId = productIds[x.ProductId];
                x.CreatedAt = x.CreatedAt.AddDays(days);
            });

            Insert(context, ReadList<ReviewImage>("ReviewImages.json"), x => x.Id, (x, id) => x.Id = id, null,
                x => x.ReviewId = reviewIds[x.ReviewId]);

            // ---------- 11. LỊCH SỬ ĐIỂM THƯỞNG ----------
            Insert(context, loyalty, x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.CustomerId = customerIds[x.CustomerId];
                x.TxnDate = x.TxnDate.AddDays(days);
                // "Tích điểm đơn hàng #{order:12}" -> "Tích điểm đơn hàng #<Id thật của đơn>"
                x.Reason = IdToken.Replace(x.Reason ?? "", m =>
                {
                    var map = m.Groups[1].Value == "order" ? orderIds : returnIds;
                    return map[int.Parse(m.Groups[2].Value)].ToString();
                });
            });

            // ---------- 12. THỐNG KÊ CỬA HÀNG (đã tính sẵn đúng theo StoreStatisticsService) ----------
            Insert(context, ReadList<StoreDailyStat>("StoreDailyStats.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.StoreId = storeIds[x.StoreId];
                x.StatDate = x.StatDate.AddDays(days);
            });

            Insert(context, ReadList<StoreItemStat>("StoreItemStats.json"), x => x.Id, (x, id) => x.Id = id, null, x =>
            {
                x.StoreId = storeIds[x.StoreId];
                x.VariantId = variantIds[x.VariantId];
                x.StatDate = x.StatDate.AddDays(days);
            });

            return $"{categoryCount} danh mục, {products.Count} sản phẩm, {variantIds.Count} biến thể, " +
                   $"{storeIds.Count} cửa hàng, {customers.Count} khách hàng, {employees.Count} nhân viên, {orders.Count} đơn hàng";
        }

        // ==================================================================================
        //  CÁC HÀM HỖ TRỢ
        // ==================================================================================

        /// <summary>
        /// Thêm danh sách vào database theo từng đợt <see cref="ChunkSize"/> dòng.
        /// Với mỗi dòng, lần lượt:
        ///   1. nhớ Id tạm trong file (getId)
        ///   2. sửa khóa ngoại / ngày giờ (fix) - lúc này Id vẫn còn là Id tạm
        ///   3. đặt Id = 0 để SQL Server tự cấp Id mới (setId)
        ///   4. lưu xong thì ghi "Id tạm -> Id mới" vào idMap (nếu có) để bảng con dùng
        /// </summary>
        private static void Insert<T>(
            ApplicationDbContext context,
            List<T> rows,
            Func<T, int> getId,
            Action<T, int> setId,
            Dictionary<int, int>? idMap,
            Action<T>? fix = null) where T : class
        {
            foreach (var chunk in rows.Chunk(ChunkSize))
            {
                var fileIds = chunk.Select(getId).ToArray();
                foreach (var row in chunk)
                {
                    fix?.Invoke(row);
                    setId(row, 0);
                }

                context.Set<T>().AddRange(chunk);
                context.SaveChanges(); // SQL Server cấp Id mới, EF tự gán ngược vào từng đối tượng

                if (idMap is not null)
                {
                    for (var i = 0; i < chunk.Length; i++)
                        idMap[fileIds[i]] = getId(chunk[i]);
                }

                context.ChangeTracker.Clear(); // "quên" các dòng đã lưu cho nhẹ bộ nhớ
            }
        }

        /// <summary>Thêm các dòng không có Id tự tăng (bảng trung gian, tài khoản...).</summary>
        private static void InsertRows<T>(ApplicationDbContext context, List<T> rows, Action<T>? fix = null) where T : class
        {
            foreach (var chunk in rows.Chunk(ChunkSize))
            {
                foreach (var row in chunk)
                    fix?.Invoke(row);

                context.Set<T>().AddRange(chunk);
                context.SaveChanges();
                context.ChangeTracker.Clear();
            }
        }

        /// <summary>Đổi khóa ngoại có thể null (int?) từ Id tạm sang Id mới.</summary>
        private static int? NewId(Dictionary<int, int> idMap, int? fileId)
            => fileId is int id ? idMap[id] : null;

        /// <summary>Đã có dữ liệu mẫu chưa? (còn tài khoản khachhang.../nhanvien...@clothesstore.vn hoặc còn kho online mẫu)</summary>
        private static bool HasSampleData(ApplicationDbContext context)
            => context.Users.Any(u => u.NormalizedEmail != null
                                      && (u.NormalizedEmail.StartsWith("KHACHHANG") || u.NormalizedEmail.StartsWith("NHANVIEN"))
                                      && u.NormalizedEmail.EndsWith(SampleEmailDomain))
               || context.Stores.Any(s => s.Name == CentralStoreName);

        /// <summary>
        /// Số ngày cần dời ngày giờ trong file cho khớp với hôm nay.
        /// Ví dụ file tạo ngày 20/10, hôm nay 25/10 -> dời +5 ngày.
        /// Nếu dời xong mà sự kiện mới nhất (đơn đặt lúc rạng sáng) vẫn nằm ở tương lai thì lùi thêm 1 ngày.
        /// </summary>
        internal static int DaysToShift(SeedInfo info, DateTime nowLocal, DateTime nowUtc)
        {
            var days = DateOnly.FromDateTime(nowLocal).DayNumber - info.AnchorDate.DayNumber;
            if (info.LatestEventUtc.AddDays(days) > nowUtc)
                days -= 1;
            return days;
        }

        /// <summary>Tạo các vai trò Customer / Employee / Admin nếu chưa có, trả về bảng "TÊN VIẾT HOA -> RoleId".</summary>
        private static Dictionary<string, string> EnsureRoles(ApplicationDbContext context)
        {
            foreach (var role in Enum.GetNames<AppRole>())
            {
                var normalized = role.ToUpperInvariant();
                if (!context.Roles.Any(r => r.NormalizedName == normalized))
                {
                    context.Roles.Add(new IdentityRole(role)
                    {
                        NormalizedName = normalized,
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }
            context.SaveChanges();
            context.ChangeTracker.Clear();

            return context.Roles
                .Where(r => r.NormalizedName != null)
                .ToList()
                .ToDictionary(r => r.NormalizedName!, r => r.Id);
        }

        /// <summary>Tạo 4 hạng mặc định (giống MembershipTierSeeder) nếu bảng hạng đang trống, trả về danh sách hạng.</summary>
        private static List<MembershipTier> EnsureTiers(ApplicationDbContext context)
        {
            if (!context.MembershipTiers.Any())
            {
                context.MembershipTiers.AddRange(
                    new MembershipTier { TierName = "Đồng", MinPoints = 0 },
                    new MembershipTier { TierName = "Bạc", MinPoints = 1000 },
                    new MembershipTier { TierName = "Vàng", MinPoints = 5000 },
                    new MembershipTier { TierName = "VIP", MinPoints = 10000 });
                context.SaveChanges();
            }

            var tiers = context.MembershipTiers.OrderBy(t => t.MinPoints).ToList();
            context.ChangeTracker.Clear();
            return tiers;
        }

        /// <summary>Hạng có MinPoints cao nhất mà số điểm đạt được (giống MembershipTierRepository.GetTierForPointsAsync).</summary>
        private static int? TierFor(List<MembershipTier> tiers, int points)
            => tiers.Where(t => t.MinPoints <= points).OrderByDescending(t => t.MinPoints).FirstOrDefault()?.Id;

        /// <summary>Mã chưa ai dùng thì giữ nguyên, trùng thì thêm đuôi -2, -3, ...</summary>
        private static string UniqueCode(string code, HashSet<string> usedCodes)
        {
            var candidate = code;
            for (var n = 2; !usedCodes.Add(candidate); n++)
                candidate = $"{code}-{n}";
            return candidate;
        }

        /// <summary>
        /// Tạo tài khoản đăng nhập (bảng AspNetUsers) + gán vai trò (AspNetUserRoles) cho khách hàng và nhân viên mẫu.
        /// Làm giống những gì UserManager.CreateAsync làm (chuẩn hóa email, băm mật khẩu...),
        /// vì lúc chạy Update-Database không có sẵn UserManager.
        /// </summary>
        private static void InsertAccounts(ApplicationDbContext context, List<SeedAccount> accounts, Dictionary<string, string> roleIds)
        {
            // Băm mật khẩu 1 lần cho mỗi loại tài khoản (việc băm cố tình chậm để chống dò mật khẩu)
            var hasher = new PasswordHasher<ApplicationUser>();
            var passwordHashOf = new Dictionary<string, string>
            {
                [nameof(AppRole.Customer)] = hasher.HashPassword(new ApplicationUser(), CustomerPassword),
                [nameof(AppRole.Employee)] = hasher.HashPassword(new ApplicationUser(), EmployeePassword)
            };

            var users = accounts.Select(a => new ApplicationUser
            {
                Id = a.Id,
                UserName = a.Email,
                NormalizedUserName = a.Email.ToUpperInvariant(),
                Email = a.Email,
                NormalizedEmail = a.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                PasswordHash = passwordHashOf[a.Role],
                SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true,
                FirstName = a.FirstName,
                LastName = a.LastName
            }).ToList();

            var userRoles = accounts.Select(a => new IdentityUserRole<string>
            {
                UserId = a.Id,
                RoleId = roleIds[a.Role.ToUpperInvariant()]
            }).ToList();

            InsertRows(context, users);
            InsertRows(context, userRoles);
        }

        // ---------- Đọc file JSON nhúng trong .dll ----------
        private static List<T> ReadList<T>(string fileName) => ReadFile<List<T>>(fileName);

        private static T ReadFile<T>(string fileName)
        {
            // Tên tài nguyên = "SeedData." + tên file (đặt trong ClothesStore.Api.csproj)
            var resourceName = "SeedData." + fileName;
            using var stream = typeof(SampleDataSeeder).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy file dữ liệu mẫu {fileName}. Kiểm tra thư mục Data/SeedData và mục EmbeddedResource trong ClothesStore.Api.csproj.");

            return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"File {fileName} rỗng.");
        }

        // ---------- Kiểu dữ liệu riêng cho 2 file không phải bảng ----------

        /// <summary>Nội dung SeedInfo.json: ngày gốc của bộ dữ liệu và thời điểm sự kiện mới nhất.</summary>
        internal sealed class SeedInfo
        {
            public DateOnly AnchorDate { get; set; }
            public DateTime LatestEventUtc { get; set; }
        }

        /// <summary>1 dòng trong Accounts.json (tài khoản đăng nhập mẫu).</summary>
        private sealed class SeedAccount
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string Role { get; set; } = string.Empty;
        }
    }
}
