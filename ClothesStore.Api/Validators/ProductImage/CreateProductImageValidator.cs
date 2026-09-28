using ClothesStore.Api.DTOs.ProductImage;
using FluentValidation;

namespace ClothesStore.Api.Validators.ProductImage
{
    public class CreateProductImageValidator : AbstractValidator<CreateProductImageDto>
    {
        public CreateProductImageValidator()
        {
            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Đường dẫn hình ảnh không được để trống")
                .MaximumLength(500).WithMessage("Đường dẫn hình ảnh không được vượt quá 500 ký tự");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId phải lớn hơn 0");
        }
    }
}
