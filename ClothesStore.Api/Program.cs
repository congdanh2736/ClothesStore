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
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//--------------------------------------------------------------[ĐĂNG KÍ DỊCH VỤ APPLICATION]---------------------------------------------------------//
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
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


//-------------------------------------------------------[ĐĂNG KÍ CÁC DỊCH VỤ SERVICE VÀ REPOSITORY]------------------------------------------------//
// Add Swagger services to generate OpenAPI specification and Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add dependency injection for repositories and services
// Auth
builder.Services.AddScoped<IAuthService, AuthService>();
// Customer
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
// Membership Tier
builder.Services.AddScoped<IMembershipTierRepository, MembershipTierRepository>();
builder.Services.AddScoped<IMembershipTierService, MembershipTierService>();
// Employee
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
//--------------------------------------------------------------------------------------------------------------------------------------------------//



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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Dùng middleware để xử lý ngoại lệ toàn cục trong ứng dụng
app.UseMiddleware<ExceptionMiddleware>();

/*
 * Xác thực và cấp quyền cho các yêu cầu HTTP
 */
app.UseAuthentication();   // xác thực token trước
app.UseAuthorization();    // rồi mới check quyền (Role)

// Dùng swagger để hiển thị các api khi chạy chương trình trên host
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
//--------------------------------------------------------------------------------------------------------------------------------------------------//