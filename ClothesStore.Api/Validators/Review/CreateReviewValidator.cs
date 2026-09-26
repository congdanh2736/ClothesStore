using ClothesStore.Api.DTOs.Review;
using FluentValidation;

namespace ClothesStore.Api.Validators.Review
{
    public class CreateReviewValidator : AbstractValidator<CreateReviewDto>
    {
        public CreateReviewValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("CustomerId phải lớn hơn 0.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId phải lớn hơn 0.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Đánh giá phải từ 1 đến 5 sao.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Bình luận không được vượt quá 1000 ký tự.");
        }
    }
}
