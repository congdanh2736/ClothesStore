using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Store_Stock
    {
        [Key]
        public int stock_id { get; set; }

        public int store_id { get; set; }
        [ForeignKey("store_id")]
        public Store? store { get; set; }

        public int variant_id { get; set; }
        [ForeignKey("variant_id")]
        public Product_Variant? product_Variant { get; set; }

        public int quantity { get; set; }
    }
}
