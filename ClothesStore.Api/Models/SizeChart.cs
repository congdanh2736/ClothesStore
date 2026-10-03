using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ClothesStore.Api.Models
{
    public class SizeChart {
        [Key]
        public int Id {get;set;}

        public string SizeLabel {get;set;} = string.Empty;
        public string Measurements { get; set; } = string.Empty;


        public int CategoryId {get; set;}
        [ForeignKey("CategoryId")]
        public Category? Category {get; set;}
    }
}