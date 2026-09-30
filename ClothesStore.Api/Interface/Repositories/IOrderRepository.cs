using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Interface.Repositories.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        
    }
}