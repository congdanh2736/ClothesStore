using ClothesStore.Api.DTOs.CollectionTech;
using FluentValidation;

namespace ClothesStore.Api.Validators.CollectionTech
{
    public class CreateCollectionTechValidator : AbstractValidator<CreateCollectionTechDto>
    {
        public CreateCollectionTechValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Tên bộ sưu tập / công nghệ không được để trống")
                .MaximumLength(150).WithMessage("Tên không được vượt quá 150 ký tự");

            RuleFor(c => c.Type)
                .NotEmpty().WithMessage("Loại không được để trống")
                .MaximumLength(100).WithMessage("Loại không được vượt quá 100 ký tự");
        }
    }
}