using ClothesStore.Api.DTOs.Category;
using FluentValidation;

namespace ClothesStore.Api.Validators
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Không được để trống ký tự")
                .MaximumLength(150).WithMessage("không được quá 150 ký tự");
        }
    }
}