using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Store
    {
        [Key]
        public int Id{ get; set; }

        public string? Name { get; set; }
        public string? Location { get; set; }

        public ICollection<StoreStock> StoreStocks { get; set; } = new List<StoreStock>();
        public ICollection<StoreDailyStat> StoreDailyStats { get; set; } = new List<StoreDailyStat>();
        public ICollection<StoreItemStat> StoreItemStats { get; set; } = new List<StoreItemStat>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
       
    }
}
