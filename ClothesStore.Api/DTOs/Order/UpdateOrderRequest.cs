using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;

namespace ClothesStore.Api.DTOs.Order
{
    public class UpdateOrderRequest
    {
        public int AddressId { get; set; }
        public int PromotionId { get; set; }
        public List<UpdateOrderItemRequest>? Items { get; set; }

    }
}