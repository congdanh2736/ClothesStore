using ClothesStore.Api.DTOs.Review;
using FluentValidation;

namespace ClothesStore.Api.Validators.Review
{
    public class UpdateReviewValidator : AbstractValidator<UpdateReviewDto>
    {
        public UpdateReviewValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Đánh giá phải từ 1 đến 5 sao.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Bình luận không được vượt quá 1000 ký tự.");
        }
    }
}
