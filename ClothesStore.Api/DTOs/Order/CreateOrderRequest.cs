using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.DTOs.Order
{
    public class CreateOrderRequest
    {
        public int CustomerId { get; set; }
        public int AddressId { get; set; }
        public int PromotionId { get; set; }
        public string? PaymentMethod { get; set; }
        public List<CreateOrderItemRequest>? Items { get; set; }
        
    }
}
