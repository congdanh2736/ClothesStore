using ClothesStore.Api.DTOs.Customer;
using FluentValidation;

namespace ClothesStore.Api.Validators.Customer
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FirstName là bắt buộc")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LastName là bắt buộc")
                .MaximumLength(100);

            RuleFor(x => x.ApplicationUserId)
                .NotEmpty().WithMessage("Customer phải liên kết với một ApplicationUser");

            RuleFor(x => x.MembershipTierId)
                .GreaterThan(0)
                .When(x => x.MembershipTierId.HasValue)
                .WithMessage("Tier là không hợp lệ");
        }
    }
}
