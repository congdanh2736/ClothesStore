using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Store_Daily_Stat
    {
        [Key]
        public int stat_id { get; set; }

        public int store_id { get; set; }
        [ForeignKey("store_id")]
        public Store? store { get; set; }

        public DateOnly stat_date { get; set; }
        public int total_orders { get; set; }
        public int completed_orders { get; set; }
        public int canceled_orders { get; set; }
        public decimal total_revenue { get; set; }
        public int total_items_sold { get; set; }
    }
}
