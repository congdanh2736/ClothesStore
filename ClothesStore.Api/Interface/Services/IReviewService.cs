using ClothesStore.Api.DTOs.Review;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IReviewService : IService<ReviewDto, CreateReviewDto, UpdateReviewDto>
    {
        Task<IEnumerable<ReviewDto>> GetByProductIdAsync(int productId);
        Task<IEnumerable<ReviewDto>> GetByCustomerIdAsync(int customerId);
    }
}
