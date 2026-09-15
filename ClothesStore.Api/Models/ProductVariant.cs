using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class ProductVariant
    {
        [Key]
        public int Variant_id {get; set;}
        public string? color {get;set;}
        public string? size {get; set;}
        public double price {get;set;}

        public int product_id {get;set;}
        [ForeignKey("product_id")]
        public Product? product {get; set;}
    
        public ICollection<CartItem> cart_Items {get;set;} = new List<CartItem>();
        public ICollection<StoreItemStat> store_Item_Stats {get; set;} = new List<StoreItemStat>();
        public ICollection<StoreStock> store_Stocks {get; set;} = new List<StoreStock>();
        public ICollection<OrderItem> Order_Items { get; set; } = new List<OrderItem>();
    }
}
