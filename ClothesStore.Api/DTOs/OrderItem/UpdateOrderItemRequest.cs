using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothesStore.Api.DTOs.OrderItem
{
    public class UpdateOrderItemRequest
    {
        public int Quantity { get; set; }
        public int VariantId { get; set; }

    }
}