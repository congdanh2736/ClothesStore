using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class PaymentTransactionProfile : Profile
    {
        public PaymentTransactionProfile()
        {
            CreateMap<PaymentTransaction, PaymentTransactionDto>();
            CreateMap<CreatePaymentTransactionRequest, PaymentTransaction>();
            CreateMap<UpdatePaymentTransactionRequest, PaymentTransaction>();
            
        }
    }
}
