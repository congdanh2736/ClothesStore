// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: AutoMapper cho Shipment
using AutoMapper;
using ClothesStore.Api.DTOs.Shipment;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ShipmentProfile : Profile
    {
        public ShipmentProfile()
        {
            CreateMap<Shipment, ShipmentDto>()
                .ForMember(dest => dest.OrderStatus,
                    opt => opt.MapFrom(src => src.Order != null ? src.Order.Status : null));
        }
    }
}
