// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: AutoMapper cho ReturnRequest / ReturnItem
using AutoMapper;
using ClothesStore.Api.DTOs.ReturnRequest;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ReturnRequestProfile : Profile
    {
        public ReturnRequestProfile()
        {
            CreateMap<ReturnRequest, ReturnRequestDto>()
                .ForMember(dest => dest.CustomerId,
                    opt => opt.MapFrom(src => src.Order != null ? src.Order.CustomerId : 0))
                .ForMember(dest => dest.TotalRefund,
                    opt => opt.MapFrom(src => src.ReturnItems.Sum(i => i.RefundAmount)))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.ReturnItems));

            CreateMap<ReturnItem, ReturnItemDto>()
                .ForMember(dest => dest.VariantId,
                    opt => opt.MapFrom(src => src.OrderItem != null ? src.OrderItem.VariantId : 0))
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.OrderItem != null && src.OrderItem.ProductVariant != null && src.OrderItem.ProductVariant.Product != null
                        ? src.OrderItem.ProductVariant.Product.Name
                        : null))
                .ForMember(dest => dest.Color,
                    opt => opt.MapFrom(src => src.OrderItem != null && src.OrderItem.ProductVariant != null ? src.OrderItem.ProductVariant.Color : null))
                .ForMember(dest => dest.Size,
                    opt => opt.MapFrom(src => src.OrderItem != null && src.OrderItem.ProductVariant != null ? src.OrderItem.ProductVariant.Size : null));
        }
    }
}
