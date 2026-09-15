using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreStock
    {
        [Key]
        public int StockID { get; set; }

        public int StoreID { get; set; }
        [ForeignKey("StoreID")]
        public Store? Store { get; set; }

        public int VariantID { get; set; }
        [ForeignKey("VariantID")]
        public ProductVariant? ProductVariant { get; set; }

        public int Quantity { get; set; }
    }
}
