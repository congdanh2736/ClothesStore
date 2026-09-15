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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity tables (AspNetUsers, AspNetRoles, ...)

            // ============ MEMBERSHIP / CUSTOMER ============
            builder.Entity<MembershipTier>(e =>
            {
                e.HasKey(x => x.TierId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            });

            builder.Entity<Customer>(e =>
            {
                e.HasKey(x => x.CustomerId);

                e.HasOne(x => x.Tier)
                    .WithMany(t => t.Customers)
                    .HasForeignKey(x => x.TierId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional 1-1 link to an Identity login account
                e.HasOne(x => x.ApplicationUser)
                    .WithOne(u => u.Customer)
                    .HasForeignKey<Customer>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Address>(e =>
            {
                e.HasKey(x => x.AddressId);

                e.HasOne(x => x.Customer)
                    .WithMany(c => c.Addresses)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ CART ============
            builder.Entity<Cart>(e =>
            {
                e.HasKey(x => x.CartId);

                // 1 customer <-> 1 cart
                e.HasOne(x => x.Customer)
                    .WithOne(c => c.Cart)
                    .HasForeignKey<Cart>(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.CustomerId).IsUnique();
            });

            builder.Entity<CartItem>(e =>
            {
                e.HasKey(x => x.CartItemId);

                e.HasOne(x => x.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(x => x.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Variant)
                    .WithMany(v => v.CartItems)
                    .HasForeignKey(x => x.VariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ WISHLIST / REVIEW ============
            builder.Entity<Wishlist>(e =>
            {
                e.HasKey(x => x.WishlistId);

                e.HasOne(x => x.Customer)
                    .WithMany(c => c.Wishlists)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.Wishlists)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // A customer can wishlist a product only once
                e.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
            });

            builder.Entity<Review>(e =>
            {
                e.HasKey(x => x.ReviewId);

                e.HasOne(x => x.Customer)
                    .WithMany(c => c.Reviews)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ STORE ============
            builder.Entity<Store>(e =>
            {
                e.HasKey(x => x.StoreId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            });

            builder.Entity<Employee>(e =>
            {
                e.HasKey(x => x.EmployeeId);

                e.HasOne(x => x.Store)
                    .WithMany(s => s.Employees)
                    .HasForeignKey(x => x.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ApplicationUser)
                    .WithOne(u => u.Employee)
                    .HasForeignKey<Employee>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<StoreStock>(e =>
            {
                e.HasKey(x => x.StockId);

                e.HasOne(x => x.Store)
                    .WithMany(s => s.StoreStocks)
                    .HasForeignKey(x => x.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Variant)
                    .WithMany(v => v.StoreStocks)
                    .HasForeignKey(x => x.VariantId)
                    .OnDelete(DeleteBehavior.Restrict);

                // One stock row per (store, variant)
                e.HasIndex(x => new { x.StoreId, x.VariantId }).IsUnique();
            });

            builder.Entity<StoreDailyStat>(e =>
            {
                e.HasKey(x => x.StatId);
                e.Property(x => x.TotalRevenue).HasPrecision(18, 2);

                e.HasOne(x => x.Store)
                    .WithMany(s => s.StoreDailyStats)
                    .HasForeignKey(x => x.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.StoreId, x.StatDate }).IsUnique();
            });

            builder.Entity<StoreItemStat>(e =>
            {
                e.HasKey(x => x.StatId);
                e.Property(x => x.TotalRevenue).HasPrecision(18, 2);

                e.HasOne(x => x.Store)
                    .WithMany(s => s.StoreItemStats)
                    .HasForeignKey(x => x.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Variant)
                    .WithMany(v => v.StoreItemStats)
                    .HasForeignKey(x => x.VariantId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.StoreId, x.VariantId, x.StatDate }).IsUnique();
            });

            // ============ CATALOG ============
            builder.Entity<Category>(e =>
            {
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(150);

                // self-referencing parent/child; Restrict to avoid multiple cascade paths
                e.HasOne(x => x.Parent)
                    .WithMany(x => x.Children)
                    .HasForeignKey(x => x.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CollectionTech>(e =>
            {
                e.HasKey(x => x.CollectionId);
                e.Property(x => x.Type).IsRequired().HasMaxLength(100);
                e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            });

            builder.Entity<Product>(e =>
            {
                e.HasKey(x => x.ProductId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);

                e.HasOne(x => x.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProductVariant>(e =>
            {
                e.HasKey(x => x.VariantId);
                e.Property(x => x.Price).HasPrecision(18, 2);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.Variants)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProductImage>(e =>
            {
                e.HasKey(x => x.ImageId);
                e.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Many-to-many Product <-> CollectionTech via composite-key join entity
            builder.Entity<ProductCollection>(e =>
            {
                e.HasKey(x => new { x.ProductId, x.CollectionId });

                e.HasOne(x => x.Product)
                    .WithMany(p => p.ProductCollections)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Collection)
                    .WithMany(c => c.ProductCollections)
                    .HasForeignKey(x => x.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ SALES ============
            builder.Entity<Promotion>(e =>
            {
                e.HasKey(x => x.PromoId);
                e.Property(x => x.Code).IsRequired().HasMaxLength(50);
                e.Property(x => x.DiscountValue).HasPrecision(18, 2);
                e.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<Order>(e =>
            {
                e.HasKey(x => x.OrderId);
                e.Property(x => x.TotalAmount).HasPrecision(18, 2);

                e.HasOne(x => x.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Address)
                    .WithMany(a => a.Orders)
                    .HasForeignKey(x => x.AddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Promotion)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(x => x.PromoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<OrderItem>(e =>
            {
                e.HasKey(x => x.OrderItemId);
                e.Property(x => x.Price).HasPrecision(18, 2);

                e.HasOne(x => x.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Variant)
                    .WithMany(v => v.OrderItems)
                    .HasForeignKey(x => x.VariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PaymentTransaction>(e =>
            {
                e.HasKey(x => x.TransactionId);
                e.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(50);
                e.Property(x => x.Status).IsRequired().HasMaxLength(50);

                // 1 order <-> 1 payment transaction
                e.HasOne(x => x.Order)
                    .WithOne(o => o.PaymentTransaction)
                    .HasForeignKey<PaymentTransaction>(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.OrderId).IsUnique();
            });
        }

    }
}
