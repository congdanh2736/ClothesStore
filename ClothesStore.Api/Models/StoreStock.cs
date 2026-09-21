using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class StoreStock
    {
        [Key]
        public int Id { get; set; }

        public int StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        public int VariantId { get; set; }
        [ForeignKey("VariantId")]
        public ProductVariant? ProductVariant { get; set; }

        public int Quantity { get; set; }
    }
}
