using AutoMapper;
using ClothesStore.Api.DTOs.Review;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repository;
        private readonly IMapper _mapper;

        public ReviewService(IReviewRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReviewDto>> GetAllAsync()
        {
            var reviews = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
        }

        public async Task<ReviewDto?> GetByIdAsync(int id)
        {
            var review = await _repository.GetByIdWithDetailsAsync(id);
            return review is null ? null : _mapper.Map<ReviewDto>(review);
        }

        public async Task<IEnumerable<ReviewDto>> GetByProductIdAsync(int productId)
        {
            var reviews = await _repository.GetByProductIdAsync(productId);
            return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
        }

        public async Task<IEnumerable<ReviewDto>> GetByCustomerIdAsync(int customerId)
        {
            var reviews = await _repository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
        }

        public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(int productId)
        {
            return await _repository.GetReviewSummaryByProductIdAsync(productId);
        }

        public async Task<bool> CanCustomerReviewProductAsync(int customerId, int productId)
        {
            if (!await _repository.CustomerExistsAsync(customerId) || !await _repository.ProductExistsAsync(productId))
                return false;

            var hasPurchased = await _repository.HasCustomerPurchasedProductAsync(customerId, productId);
            if (!hasPurchased) return false;

            var hasReviewed = await _repository.GetByCustomerAndProductAsync(customerId, productId) != null;
            return !hasReviewed;
        }

        public async Task<(bool Success, string? Error, ReviewDto? Data)> CreateAsync(CreateReviewDto dto)
        {
            if (!await _repository.CustomerExistsAsync(dto.CustomerId))
                return (false, "Khách hàng không tồn tại.", null);

            if (!await _repository.ProductExistsAsync(dto.ProductId))
                return (false, "Sản phẩm không tồn tại.", null);

            // Kiểm tra khách hàng đã mua sản phẩm chưa
            if (!await _repository.HasCustomerPurchasedProductAsync(dto.CustomerId, dto.ProductId))
                return (false, "Khách hàng chỉ có thể đánh giá sản phẩm sau khi đã mua hàng thành công.", null);

            // Kiểm tra xem khách hàng đã đánh giá sản phẩm này chưa
            var existing = await _repository.GetByCustomerAndProductAsync(dto.CustomerId, dto.ProductId);
            if (existing != null)
                return (false, "Khách hàng đã đánh giá sản phẩm này rồi.", null);

            var review = _mapper.Map<Review>(dto);
            if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
            {
                review.Images = dto.ImageUrls
                    .Take(3)
                    .Select(url => new ReviewImage { ImageUrl = url })
                    .ToList();
            }
            await _repository.AddAsync(review);

            var created = await _repository.GetByIdWithDetailsAsync(review.Id);
            return (true, null, _mapper.Map<ReviewDto>(created));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateReviewDto dto)
        {
            var review = await _repository.GetByIdWithDetailsAsync(id);
            if (review is null) return (false, "Không tìm thấy đánh giá.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            if (dto.ImageUrls != null)
            {
                review.Images.Clear();
                foreach (var url in dto.ImageUrls.Take(3))
                {
                    review.Images.Add(new ReviewImage { ImageUrl = url, ReviewId = review.Id });
                }
            }

            await _repository.UpdateAsync(review);
            return (true, null);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var review = await _repository.GetByIdAsync(id);
            if (review is null) return false;
            await _repository.DeleteAsync(review);
            return true;
        }
    }
}
