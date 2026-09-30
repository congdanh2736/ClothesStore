using ClothesStore.Api.DTOs.ProductVariant;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IProductVariantService : IService<ProductVariantDto, CreateProductVariantDto, UpdateProductVariantDto>
    {
        
    }
}