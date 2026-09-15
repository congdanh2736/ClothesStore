using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.src.Models
{
    public class Product_Image
    {
        [Key]
        public int Image_id {get; set;}
        public string? image_url {get; set;}
        public int collection_id {get;set;}
        [ForeignKey("collection_id")]
        public Product? products {get; set;}
    }
}
