using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreDailyStat
    {
        [Key]
        public int StatID { get; set; }

        public int StoreID { get; set; }
        [ForeignKey("StoreID")]
        public Store? Store { get; set; }

        public DateOnly Statdate { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
