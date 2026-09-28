using ClothesStore.Api.DTOs.Address;
using FluentValidation;

namespace ClothesStore.Api.Validators.Address
{
    public class UpdateAddressValidator : AbstractValidator<UpdateAddressDto>
    {
        public UpdateAddressValidator()
        {
            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Cần phải có số nhà và tên đường")
                .MaximumLength(100).WithMessage("Đường không được quá 100 ký tự");

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("Quận/Huyện không được để trống")
                .MaximumLength(50).WithMessage("Quận/Huyện không được quá 50 ký tự");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Thành phố không được để trống")
                .MaximumLength(50).WithMessage("Thành phố không được quá 50 ký tự");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Quốc gia không được để trống")
                .MaximumLength(50).WithMessage("Quốc gia không được quá 50 ký tự");
        }
    }
}
