// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
using AutoMapper;
using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class PaymentTransactionProfile : Profile
    {
        public PaymentTransactionProfile()
        {
            // [SỬA] Map tổng tiền đơn hàng (trước đây: CreateMap<PaymentTransaction, PaymentTransactionDto>();)
            CreateMap<PaymentTransaction, PaymentTransactionDto>()
                .ForMember(dest => dest.OrderTotal,
                    opt => opt.MapFrom(src => src.Order != null ? src.Order.TotalAmount : 0m));

            CreateMap<CreatePaymentTransactionRequest, PaymentTransaction>();
            // [XÓA] Service tự gán PaymentMethod nên bỏ map này CreateMap<UpdatePaymentTransactionRequest, PaymentTransaction>();
        }
    }
}
