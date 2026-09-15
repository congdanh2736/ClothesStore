using Microsoft.EntityFrameworkCore;

namespace ClothesStore.src.Models
{
    public class MyDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Store_Stock> Store_Stocks { get; set; }
        public DbSet<Store_Daily_Stat> Store_Daily_Stats { get; set; }
        public DbSet<Store_Item_Stat> Store_Item_Stats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseInMemoryDatabase("ClothesStoreDb");
        }
    }
}
