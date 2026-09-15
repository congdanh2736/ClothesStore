using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Customer domain
        public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<Review> Reviews => Set<Review>();

        // Store domain
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<StoreStock> StoreStocks => Set<StoreStock>();
        public DbSet<StoreDailyStat> StoreDailyStats => Set<StoreDailyStat>();
        public DbSet<StoreItemStat> StoreItemStats => Set<StoreItemStat>();

        // Catalog domain
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CollectionTech> CollectionTechs => Set<CollectionTech>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductCollection> ProductCollections => Set<ProductCollection>();

        // Sales domain
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    }
}
