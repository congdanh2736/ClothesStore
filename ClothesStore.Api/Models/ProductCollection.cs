using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    //class trung gian của collection_tech với products
    public class ProductCollection
    {
        public int ProductId {get;set;}
        [ForeignKey("ProductId")]
        public Product? Product {get; set;}

        public int CollectionId {get; set;}
        [ForeignKey("CollectionId")]
        public CollectionTech? CollectionTech {get; set;}
    }
}
