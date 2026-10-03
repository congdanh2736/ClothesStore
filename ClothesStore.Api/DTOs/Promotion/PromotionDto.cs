using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace ClothesStore.Api.DTOs.Promotion
{
    public class PromotionDto
    {
        public int Id { get; set;}
        public string Code { get; set; } = string.Empty;
        public decimal DiscountValue { get; set;}
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}