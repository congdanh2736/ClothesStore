// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Linq; System.Threading.Tasks;
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
                    opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}"
                        : string.Empty))
                .ForMember(dest => dest.Address,
                    opt => opt.MapFrom(src => src.Address == null
                        ? string.Empty
                        : string.Join(", ", new[] { src.Address.Street, src.Address.District, src.Address.City, src.Address.Country }
                            .Where(part => !string.IsNullOrWhiteSpace(part)))))
                // [THÊM] ===== BẮT ĐẦU: map các trường mới của OrderDetailDTO =====
                .ForMember(dest => dest.PromotionCode,
                    opt => opt.MapFrom(src => src.Promotion != null ? src.Promotion.Code : null))
                .ForMember(dest => dest.StoreName,
                    opt => opt.MapFrom(src => src.Store != null ? src.Store.Name : null))
                // [THÊM] ===== KẾT THÚC =====
                .ForMember(dest => dest.PaymentMethod,
                    opt => opt.MapFrom(src => src.PaymentTransaction == null ? null : src.PaymentTransaction.PaymentMethod))
                // [THÊM] ===== BẮT ĐẦU =====
                .ForMember(dest => dest.PaymentStatus,
                    opt => opt.MapFrom(src => src.PaymentTransaction == null ? null : src.PaymentTransaction.Status))
                .ForMember(dest => dest.ShipmentStatus,
                    opt => opt.MapFrom(src => src.Shipment == null ? null : src.Shipment.Status))
                .ForMember(dest => dest.TrackingNumber,
                    opt => opt.MapFrom(src => src.Shipment == null ? null : src.Shipment.TrackingNumber))
                .ForMember(dest => dest.Subtotal,
                    opt => opt.MapFrom(src => src.TotalAmount + src.DiscountAmount));
                // [THÊM] ===== KẾT THÚC =====

            // [XÓA] CreateMap<CreateOrderRequest, Order>() (+ Ignore OrderItems, PaymentTransaction):
            //       OrderService giờ tạo Order trực tiếp (tính giá, khuyến mãi, cửa hàng) nên không cần map này
        }
    }
}
