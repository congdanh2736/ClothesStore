using ClothesStore.Api.DTOs.ProductImage;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IProductImageService : IService<ProductImageDto, CreateProductImageDto, UpdateProductImageDto>
    {

    }
}