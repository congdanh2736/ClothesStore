using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    //class trung gian của collection_tech với products
    public class Product_Collection
    {
        public int product_id {get;set;}
        [ForeignKey("product_id")]
        public Product? products {get; set;}

        public int Collection_id {get; set;}
        [ForeignKey("collection_id")]
        public Collection_Tech? collection_Techs {get; set;}
    }
}
