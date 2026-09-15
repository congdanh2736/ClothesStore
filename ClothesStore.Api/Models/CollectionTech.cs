using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class CollectionTech
    {
        [Key]
        public int Id {get; set;}

        //type gồm: colection hoặc Techology
        public string? Type {get; set;}
        //tên của colection hoặc Techology
        public string? Name {get; set;} 
        public ICollection<ProductCollection> ProductCollections { get; set;} = new List<ProductCollection>();
    }
}
