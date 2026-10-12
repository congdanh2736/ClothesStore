using AutoMapper;
using ClothesStore.Api.DTOs.StoreStock;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class StoreStockProfile : Profile
    {
        public StoreStockProfile()
        {
            // [SỬA] Map tên cửa hàng, tên sản phẩm, màu, size (trước đây: CreateMap<StoreStock, StoreStockDto>();) ===== BẮT ĐẦU =====
            CreateMap<StoreStock, StoreStockDto>()
                .ForMember(dest => dest.StoreName,
                    opt => opt.MapFrom(src => src.Store != null ? src.Store.Name : null))
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.ProductVariant != null && src.ProductVariant.Product != null
                        ? src.ProductVariant.Product.Name
                        : null))
                .ForMember(dest => dest.Color,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Color : null))
                .ForMember(dest => dest.Size,
                    opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.Size : null));
            // [SỬA] ===== KẾT THÚC =====

            CreateMap<CreateStoreStockDto, StoreStock>();
            CreateMap<UpdateStoreStockDto, StoreStock>();
        }
    }
}
