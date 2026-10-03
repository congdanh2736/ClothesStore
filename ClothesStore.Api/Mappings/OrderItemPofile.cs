using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class OrderItemPofile : Profile
    {
        public OrderItemPofile()
        {
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(dest => dest.OrderItemId,
                    opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VariantName,
                    opt => opt.MapFrom(src => src.ProductVariant != null && src.ProductVariant.Product != null
                        ? src.ProductVariant.Product.Name
                        : null));
                
            CreateMap<CreateOrderItemRequest, OrderItem>();
            CreateMap<UpdateOrderItemRequest, OrderItem>();

        }
    }
}
