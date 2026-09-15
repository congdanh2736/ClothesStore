using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    //class trung gian của collection_tech với products
    public class ProductCollection
    {
        public int product_id {get;set;}
        [ForeignKey("product_id")]
        public Product? products {get; set;}

        public int Collection_id {get; set;}
        [ForeignKey("collection_id")]
        public CollectionTech? collection_Techs {get; set;}
    }
}
