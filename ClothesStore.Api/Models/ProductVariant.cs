using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId {get; set;}
        public string? Color {get;set;}
        public string? Size {get; set;}
        public double Price {get;set;}

        public int ProductId {get;set;}
        [ForeignKey("ProductId")]
        public Product? Product {get; set;}
    
        public ICollection<CartItem> CartItems {get;set;} = new List<CartItem>();
        public ICollection<StoreItemStat> StoreItemStats {get; set;} = new List<StoreItemStat>();
        public ICollection<StoreStock> StoreStocks {get; set;} = new List<StoreStock>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
