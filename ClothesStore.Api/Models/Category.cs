using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.src.Models
{
    public class Category {
        [Key]
        public int Category_id { get; set; }
        public string? name { get; set; }

        public int? Parent_Category_id {get; set;}
        [ForeignKey("Parent_Category_id")]
        public Category? Parent_Category {get; set;}

        public ICollection<Product>? products {get; set;} = new List<Product>();
    }
}
