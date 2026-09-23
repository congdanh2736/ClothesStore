using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace ClothesStore.Api.Common
{
    // Tạo một Admin mặc định khi khởi động ứng dụng, chỉ dùng để test, nên đổi ngay sau khi đăng nhập lần đầu
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            // Lấy UserManager từ DI container
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            const string adminEmail = "admin@clothesstore.com";
            const string adminPassword = "Admin@123456"; // chỉ dùng tạm để test, đổi ngay sau khi đăng nhập lần đầu

            // Kiểm tra xem đã có Admin tồn tại chưa
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin is not null) return; // đã có Admin rồi, không tạo lại

            // Tạo một Admin mới
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            // Tạo Admin với mật khẩu mặc định
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                // Gán role Admin cho user vừa tạo
                await userManager.AddToRoleAsync(admin, nameof(AppRole.Admin));
            }
        }
    }
}