using Microsoft.EntityFrameworkCore;

namespace ClothesStore.src.Models
{
    public class MyDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseInMemoryDatabase("ClothesStoreDb");
        }
    }
}
