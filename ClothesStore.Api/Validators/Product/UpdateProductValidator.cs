using ClothesStore.Api.DTOs.Product;
using FluentValidation;

namespace ClothesStore.Api.Validators.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
                .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự");

            RuleFor(c => c.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId phải lớn hơn 0");
        }
    }
}