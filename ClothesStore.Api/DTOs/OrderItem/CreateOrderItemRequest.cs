using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothesStore.Api.DTOs.OrderItem
{
    public class CreateOrderItemRequest
    {
       
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        
    }
}