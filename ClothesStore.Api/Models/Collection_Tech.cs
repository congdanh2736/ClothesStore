using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.src.Models
{
    public class Collection_Tech
    {
        [Key]
        public int Collection_id {get; set;}

        //type gồm: colection hoặc Techology
        public string? type {get; set;}
        //tên của colection hoặc Techology
        public string? name {get; set;} 
        public ICollection<Product_Collection> product_Collections { get; set;} = new List<Product_Collection>();
    }
}
