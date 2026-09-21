using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesStore.Api.Models
{
    public class Category {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }

        public int? ParentCategoryId {get; set;}
        [ForeignKey("ParentCategoryId")]
        public Category? ParentCategory {get; set;}

        
        public ICollection<Category>? ChildrenCategories { get; set; } = new List<Category>();
        public ICollection<Product>? Products {get; set;} = new List<Product>();
    }
}
