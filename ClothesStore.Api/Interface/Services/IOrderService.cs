using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IOrderService : IService< OrderDetailDTO, CreateOrderRequest, UpdateOrderRequest> 
    {
        
    }
}