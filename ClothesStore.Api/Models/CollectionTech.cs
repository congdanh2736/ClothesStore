using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class CollectionTech
    {
        [Key]
        public int Collection_id {get; set;}

        //type gồm: colection hoặc Techology
        public string? type {get; set;}
        //tên của colection hoặc Techology
        public string? name {get; set;} 
        public ICollection<ProductCollection> product_Collections { get; set;} = new List<ProductCollection>();
    }
}
