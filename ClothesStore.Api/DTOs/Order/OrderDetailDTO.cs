using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.DTOs.OrderItem;

namespace ClothesStore.Api.DTOs.Order
{
    // Chi tiet don hang
    public class OrderDetailDTO
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AddressId { get; set; }
        public string Address { get; set; } = string.Empty;
        public int PromotionId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDTO> OrderItems { get; set; } = new();
        
    }
}