using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Store
    {
        [Key]
        public int store_id { get; set; }

        public string? name { get; set; }
        public string? location { get; set; }

        public ICollection<Store_Stock> store_Stocks { get; set; } = new List<Store_Stock>();
        public ICollection<Store_Daily_Stat> store_Daily_Stats { get; set; } = new List<Store_Daily_Stat>();
        public ICollection<Store_Item_Stat> store_Item_Stats { get; set; } = new List<Store_Item_Stat>();
    }
}
