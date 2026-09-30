using ClothesStore.Api.DTOs.Category;
using FluentValidation;

namespace ClothesStore.Api.Validators
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>{
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục Không được để trống ký tự")
                .MaximumLength(150).WithMessage("Tên danh mục không được quá 150 ký tự");
        }
    }
}