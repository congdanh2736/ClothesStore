using ClothesStore.Api.DTOs.Product;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IProductService : IService<ProductDto,CreateProductDto,UpdateProductDto>{
        
    }
}