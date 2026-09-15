using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreItemStat
    {
        [Key]
        public int stat_id { get; set; }

        public int store_id { get; set; }
        [ForeignKey("store_id")]
        public Store? store { get; set; }

        public int variant_id { get; set; }
        [ForeignKey("variant_id")]
        public ProductVariant? product_Variant { get; set; }

        public DateOnly stat_date { get; set; }
        public int quantity_sold { get; set; }
        public decimal total_revenue { get; set; }
        public int return_quantity { get; set; }
    }
}
