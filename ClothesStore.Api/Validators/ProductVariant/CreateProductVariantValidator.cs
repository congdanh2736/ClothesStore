using ClothesStore.Api.DTOs.ProductVariant;
using FluentValidation;

namespace ClothesStore.Api.Validators.ProductVariant
{
    public class CreateProductVariantValidator : AbstractValidator<CreateProductVariantDto>
    {
        public CreateProductVariantValidator()
        {
            RuleFor(x => x.Color)
                .NotEmpty().WithMessage("Màu sắc không được để trống")
                .MaximumLength(50).WithMessage("Màu sắc không được vượt quá 50 ký tự");

            RuleFor(x => x.Size)
                .NotEmpty().WithMessage("Kích cỡ không được để trống")
                .MaximumLength(20).WithMessage("Kích cỡ không được vượt quá 20 ký tự");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId phải lớn hơn 0");
        }
    }
}
