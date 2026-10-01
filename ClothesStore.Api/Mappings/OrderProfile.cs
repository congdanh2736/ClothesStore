using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
             CreateMap<Order, OrderDetailDTO>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null  ? $"{src.Customer.FirstName} {src.Customer.LastName}"
                        : string.Empty))
                .ForMember(dest => dest.Address,
                    opt => opt.MapFrom(src => src.Address == null
                        ? string.Empty
                        : string.Join(", ", new[] { src.Address.Street, src.Address.District, src.Address.City, src.Address.Country }
                            .Where(part => !string.IsNullOrWhiteSpace(part)))) )
                .ForMember(dest => dest.PaymentMethod,
                    opt => opt.MapFrom(src => src.PaymentTransaction == null ? null : src.PaymentTransaction.PaymentMethod));

            CreateMap<CreateOrderRequest, Order>()
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentTransaction, opt => opt.Ignore());
        }
        
    }
}
