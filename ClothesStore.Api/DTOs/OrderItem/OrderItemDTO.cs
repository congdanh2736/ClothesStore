using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothesStore.Api.DTOs.OrderItem
{
    public class OrderItemDTO
    {
        public int OrderItemId { get; set; }
        public int VariantId { get; set ; }
        public string? VariantName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    
    }
}