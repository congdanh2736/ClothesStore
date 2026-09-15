using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.src.Models
{
    public class Product_Variant
    {
        [Key]
        public int Variant_id {get; set;}
        public string? color {get;set;}
        public string? size {get; set;}
        public double price {get;set;}

        public int product_id {get;set;}
        [ForeignKey("product_id")]
        public Product? product {get; set;}
    
        public ICollection<Cart_Item> cart_Items {get;set;} = new List<Cart_Item>();
        public ICollection<Store_Item_Stat> store_Item_Stats {get; set;} = new List<Store_Item_Stat>();
        public ICollection<Store_Stock> store_Stocks {get; set;} = new List<Store_Stock>();
        public ICollection<Order_Item> Order_Items { get; set; } = new List<Order_Item>();
    }
}
