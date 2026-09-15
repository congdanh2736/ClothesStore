using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId {get; set;}
        public string? ImageUrl {get; set;}
        public int CollectionId {get;set;}
        [ForeignKey("CollectionId")]
        public Product? Product {get; set;}
    }
}
