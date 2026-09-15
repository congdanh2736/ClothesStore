using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreItemStat
    {
        [Key]
        public int StatID { get; set; }

        public int StoreID { get; set; }
        [ForeignKey("StoreID")]
        public Store? Store { get; set; }

        public int VariantID { get; set; }
        [ForeignKey("VariantID")]
        public ProductVariant? ProductVariant { get; set; }

        public DateOnly Statdate { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ReturnQuantity { get; set; }
    }
}
