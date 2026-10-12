using ClothesStore.Api.Data;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Common
{
    // Tạo sẵn các hạng thành viên mặc định (Đồng, Bạc, Vàng, VIP) nếu bảng MembershipTiers đang trống
    public static class MembershipTierSeeder
    {
        public static async Task SeedTiersAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.MembershipTiers.AnyAsync()) return;

            context.MembershipTiers.AddRange(
                new MembershipTier { TierName = "Đồng", MinPoints = 0 },
                new MembershipTier { TierName = "Bạc", MinPoints = 1000 },
                new MembershipTier { TierName = "Vàng", MinPoints = 5000 },
                new MembershipTier { TierName = "VIP", MinPoints = 10000 }
            );
            await context.SaveChangesAsync();
        }
    }
}
