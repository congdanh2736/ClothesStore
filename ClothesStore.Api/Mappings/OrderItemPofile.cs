// [XÓA] using không còn dùng: System; System.Collections.Generic; System.Globalization; System.Linq; System.Threading.Tasks;
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
                // [THÊM] map ProductId
                .ForMember(dest => dest.ProductId,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.ProductId : 0))
                .ForMember(dest => dest.VariantName,
                    opt => opt.MapFrom(src => src.ProductVariant != null && src.ProductVariant.Product != null
                        ? src.ProductVariant.Product.Name
                        : null))
                // [THÊM] map màu, size của biến thể
                .ForMember(dest => dest.Color,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Color : null))
                .ForMember(dest => dest.Size,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Size : null));

            // [XÓA] CreateMap<CreateOrderItemRequest, OrderItem>(); CreateMap<UpdateOrderItemRequest, OrderItem>();
            //       OrderItemService giờ tự gán từng trường (để giữ giá cũ khi chỉ đổi số lượng) nên không cần map
        }
    }
}
