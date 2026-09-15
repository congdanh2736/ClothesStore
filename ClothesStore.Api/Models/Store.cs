using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Store
    {
        [Key]
        public int StoreID { get; set; }

        public string? Name { get; set; }
        public string? Location { get; set; }

        public ICollection<StoreStock> storeStocks { get; set; } = new List<StoreStock>();
        public ICollection<StoreDailyStat> storeDailyStats { get; set; } = new List<StoreDailyStat>();
        public ICollection<StoreItemStat> storeItemStats { get; set; } = new List<StoreItemStat>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
       
    }
}
