using AutoMapper;
using AutoMapper.Configuration;
using ClothesStore.Api.Common;
using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Mappings;
using ClothesStore.Api.Middleware;
using ClothesStore.Api.Models;
using ClothesStore.Api.Repositories;
using ClothesStore.Api.Services;
using ClothesStore.Api.Validators.Customer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // [THÊM] PhysicalFileProvider: đọc file giao diện trực tiếp từ ổ đĩa
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

/*
 * [THÊM] Không in từng câu lệnh SQL của EF Core ra cửa sổ console.
 * Lý do: lúc thêm dữ liệu mẫu (~55.000 dòng) EF in ra hàng nghìn câu INSERT rất dài,
 * làm chương trình và Package Manager Console (Update-Database) chạy rất chậm.
 * Câu lệnh SQL bị LỖI vẫn được in ra bình thường.
 * Muốn xem lại câu SQL khi gỡ lỗi: thêm // vào đầu dòng bên dưới.
 */
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//--------------------------------------------------------------[ĐĂNG KÍ DỊCH VỤ APPLICATION]---------------------------------------------------------//
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
        /*
         * [THÊM] Dữ liệu mẫu: mỗi lần chạy Update-Database (hoặc dotnet ef database update),
         * EF Core tự gọi SampleDataSeeder để thêm dữ liệu mẫu (database đã có rồi thì bỏ qua).
         * - UseSeeding     : bản thường, dùng cho Update-Database
         * - UseAsyncSeeding: bản async, dùng khi code gọi Database.MigrateAsync()
         * Xem chi tiết trong Common/SampleDataSeeder.cs
         */
        .UseSeeding((context, _) => SampleDataSeeder.Seed((ApplicationDbContext)context))
        .UseAsyncSeeding((context, _, cancellationToken) =>
            SampleDataSeeder.SeedAsync((ApplicationDbContext)context, cancellationToken)));
//----------------------------------------------------------------------------------------------------------------------------------------------------//


//-------------------------------------------------------------[ĐĂNG KÍ DỊCH VỤ IDENTITY]-------------------------------------------------------------//
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
//----------------------------------------------------------------------------------------------------------------------------------------------------//


//-----------------------------------------------------------------[ĐĂNG KÍ DỊCH VỤ JWT]--------------------------------------------------------------//
/*
 * Thêm dịch vụ JWT Authentication vào ứng dụng
 * Ứng dụng JWT Authentication sẽ giúp xác thực người dùng dựa trên token JWT được gửi từ client
 * Giúp bảo mật
 */
builder.Services.AddAuthentication(options =>
{
    /*
     * Cấu hình mặc định cho xác thực và challenge sử dụng JWT Bearer
     * JwtBearerDefaults.AuthenticationScheme là một hằng số định nghĩa chuỗi "Bearer" để xác định loại xác thực mà ứng dụng sẽ sử dụng
     */
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Cấu hình JWT Bearer
.AddJwtBearer(options =>
{
    //Cấu hình các tham số xác thực token JWT
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Xác thực issuer (người phát hành token)
        ValidateAudience = true, // Xác thực audience (người nhận token)
        ValidateLifetime = true, // Xác thực thời gian sống của token

        /* 
         * Xác thực khóa ký của issuer
         * Khóa ký là gì? Khóa ký là một chuỗi bí mật được sử dụng để ký token JWT, đảm bảo rằng token không bị giả mạo
         */
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Xác thực issuer (người phát hành token) dựa trên cấu hình trong appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"], // Xác thực audience (người nhận token) dựa trên cấu hình trong appsettings.json
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // Khóa ký được tạo từ chuỗi bí mật trong cấu hình, sử dụng thuật toán HMAC SHA256
    };
});
//--------------------------------------------------------------------------------------------------------------------------------------------------//


//------------------------------------------------------------[ĐĂNG KÍ DỊCH VỤ SWAGGER]-------------------------------------------------------------//
// Add Swagger services to generate OpenAPI specification and Swagger UI
builder.Services.AddEndpointsApiExplorer();

// SchemeId là tên của scheme xác thực mà Swagger sẽ sử dụng để xác thực các yêu cầu API.
const string schemeId = "Bearer";

/*
 * Cấu hình Swagger để hỗ trợ xác thực JWT Bearer
 * Khi người dùng truy cập Swagger UI, họ sẽ thấy một nút "Authorize" để nhập token JWT
 * Token này sẽ được gửi trong header Authorization của các yêu cầu API
 */
builder.Services.AddSwaggerGen(options =>
{
    // Thêm định nghĩa scheme xác thực Bearer vào Swagger
    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập token theo dạng: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",       // chữ thường, theo chuẩn RFC 7235
        BearerFormat = "JWT"
    });

    // Thêm yêu cầu xác thực vào Swagger, yêu cầu tất cả các endpoint phải có token JWT
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(schemeId, document),
            new List<string>()
        }
    });
});
//--------------------------------------------------------------------------------------------------------------------------------------------------//


//-------------------------------------------------------[ĐĂNG KÍ CÁC DỊCH VỤ SERVICE VÀ REPOSITORY]------------------------------------------------//
// Add dependency injection for repositories and services
// Jwt
builder.Services.AddScoped<IJwtService, JwtService>();
// Auth
builder.Services.AddScoped<IAuthService, AuthService>();
// Customer
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
// Employee
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
// Address
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IAddressService, AddressService>();
// LoyaltyTransaction
builder.Services.AddScoped<ILoyaltyTransactionRepository, LoyaltyTransactionRepository>();
builder.Services.AddScoped<ILoyaltyTransactionService, LoyaltyTransactionService>();
// Membership Tier
builder.Services.AddScoped<IMembershipTierRepository, MembershipTierRepository>();
builder.Services.AddScoped<IMembershipTierService, MembershipTierService>();
// Store and inventory
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IStoreStockRepository, StoreStockRepository>();
builder.Services.AddScoped<IStoreStockService, StoreStockService>();
builder.Services.AddScoped<IStoreDailyStatRepository, StoreDailyStatRepository>();
builder.Services.AddScoped<IStoreDailyStatService, StoreDailyStatService>();
builder.Services.AddScoped<IStoreItemStatRepository, StoreItemStatRepository>();
builder.Services.AddScoped<IStoreItemStatService, StoreItemStatService>();


// Cart
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<ICartService, CartService>();
// Wishlist
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
// Review
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();
// Category
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
// Collection_Tech
builder.Services.AddScoped<ICollectionTechRepository, CollectionTechRepository>();
builder.Services.AddScoped<ICollectionTechService, CollectionTechService>();
// Product
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
// Product_Variant
builder.Services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
// Product_Image
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();

// Size_Chart
builder.Services.AddScoped<ISizeChartRepository, SizeChartRepository>();
builder.Services.AddScoped<ISizeChartService, SizeChartService>();

//--------------------------------------------------------------------------------------------------------------------------------------------------//

// Order - Payment - Promotion
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();

// Return_Request + Return_Item (bảng 17, 18)
builder.Services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
builder.Services.AddScoped<IReturnRequestService, ReturnRequestService>();
// [SỬA LỖI] Shipment (vận chuyển) đã có Controller + Service + Repository nhưng chưa đăng ký ở đây
//           -> gọi bất kỳ API /api/Shipment nào cũng bị lỗi 500 "Unable to resolve service"
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
// Nghiệp vụ dùng chung: tồn kho & thống kê cửa hàng tự động
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IStoreStatisticsService, StoreStatisticsService>();



//----------------------------------------------------[CẤU HÌNH AUTO MAPPER VÀ FLUENT VALIDATION]-------------------------------------------------//
// Add AutoMapper and FluentValidation services
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();
builder.Services.AddFluentValidationAutoValidation();
//--------------------------------------------------------------------------------------------------------------------------------------------------//


//-------------------------------------------------------------[CẤU HÌNH APPLICATION]---------------------------------------------------------------//
var app = builder.Build();

// Gán role và tạo admin mặc định khi ứng dụng khởi động
using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
    await AdminSeeder.SeedAdminAsync(scope.ServiceProvider);
    await MembershipTierSeeder.SeedTiersAsync(scope.ServiceProvider); // Đồng / Bạc / Vàng / VIP

    // [THÊM] Dữ liệu mẫu (danh mục, sản phẩm, khách hàng, đơn hàng...) nếu database chưa có.
    // Bình thường dữ liệu đã được thêm lúc chạy Update-Database; dòng này để phòng khi quên.
    // Đã có dữ liệu mẫu rồi thì hàm chỉ kiểm tra rồi bỏ qua, không làm chậm lúc khởi động.
    SampleDataSeeder.Seed(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        message => app.Logger.LogInformation("{Message}", message));
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Dùng middleware để xử lý ngoại lệ toàn cục trong ứng dụng
app.UseMiddleware<ExceptionMiddleware>();

// Dùng swagger để hiển thị các api khi chạy chương trình trên host
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

/*
 * [THÊM] Phục vụ giao diện frontend (HTML/CSS/JS) nằm trong thư mục wwwroot
 * - UseDefaultFiles: mở địa chỉ gốc "/" thì tự trả về wwwroot/index.html
 * - UseStaticFiles : cho phép trình duyệt tải các file trong wwwroot (html, css, js, ảnh)
 * Phải đặt UseDefaultFiles TRƯỚC UseStaticFiles.
 * Frontend chạy chung địa chỉ với API nên không cần cấu hình CORS.
 *
 * [SỬA] Chỉ định RÕ thư mục wwwroot bằng PhysicalFileProvider (đọc thẳng file trên ổ đĩa).
 * Lý do: ở chế độ Development, .NET 10 mặc định đọc danh sách file tĩnh từ một "bản ghi"
 * tạo lúc build. Bản ghi cũ (thêm file vào wwwroot sau khi build) -> server báo 404
 * dù file có thật. Đọc thẳng từ ổ đĩa thì thêm/sửa file là dùng được ngay, không phụ thuộc build.
 */
var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");

// In ra console đường dẫn wwwroot đang dùng, để dễ kiểm tra khi trang báo 404
app.Logger.LogInformation("Thư mục giao diện (wwwroot): {Path} | Có admin/login.html: {Exists}",
    webRootPath, File.Exists(Path.Combine(webRootPath, "admin", "login.html")));

if (Directory.Exists(webRootPath))
{
    var webRootFiles = new PhysicalFileProvider(webRootPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = webRootFiles });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = webRootFiles,
        /*
         * [SỬA] Bắt trình duyệt luôn hỏi lại server xem file (html, css, js) có đổi chưa.
         * Lý do: trước đây server không gửi kèm "Cache-Control", trình duyệt tự giữ bản cũ
         * (ví dụ dashboard.js cũ) một thời gian -> sửa file xong F5 vẫn chạy code cũ.
         * "no-cache" KHÔNG phải là không lưu: file không đổi thì server trả 304 (rất nhanh),
         * file đã đổi thì trình duyệt tải bản mới ngay.
         */
        OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache"
    });
}
else
{
    // Không có thư mục wwwroot (đặt sai chỗ) -> báo rõ trong console thay vì im lặng trả 404
    app.Logger.LogWarning("Không tìm thấy thư mục wwwroot tại {Path}. Giao diện web sẽ không mở được.", webRootPath);
}

/*
 * Xác thực và cấp quyền cho các yêu cầu HTTP
 * (trước đây gọi UseAuthorization 2 lần và UseHttpsRedirection nằm sau xác thực)
 */
app.UseAuthentication();   // xác thực token trước
app.UseAuthorization();    // rồi mới check quyền (Role)

app.MapControllers();

app.Run();
//--------------------------------------------------------------------------------------------------------------------------------------------------//