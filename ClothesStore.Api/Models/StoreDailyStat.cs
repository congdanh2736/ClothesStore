using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreDailyStat
    {
        [Key]
        public int Id { get; set; }

        public int StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        public DateOnly StatDate { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
