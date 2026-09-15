using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Product
    {
        [Key]
        public int product_id {get; set;}
        public string? name {get; set;}

        public int Category_id {get; set;}
        [ForeignKey("Category_id")]
        public Category? categorys {get; set;}

        public ICollection<ProductVariant> product_Variants {get; set;} = new List<ProductVariant>();
        public ICollection<ProductImage> product_Images {get; set;} = new List<ProductImage>();
        public ICollection<ProductCollection> product_Collections {get; set;} = new List<ProductCollection>();
        public ICollection<Wishlist> wishlists {get; set;} = new List<Wishlist>();
        public ICollection<Review> reviews {get; set;} = new List<Review>();
    }
}
