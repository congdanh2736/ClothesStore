using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Store
    {
        [Key]
        public int store_id { get; set; }

        public string? name { get; set; }
        public string? location { get; set; }

        public ICollection<StoreStock> store_Stocks { get; set; } = new List<StoreStock>();
        public ICollection<StoreDailyStat> store_Daily_Stats { get; set; } = new List<StoreDailyStat>();
        public ICollection<StoreItemStat> store_Item_Stats { get; set; } = new List<StoreItemStat>();
    }
}
