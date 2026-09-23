using ClothesStore.Api.DTOs.Customer;
using FluentValidation;

namespace ClothesStore.Api.Validators.Customer
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FirstName is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LastName is required")
                .MaximumLength(100);

            RuleFor(x => x.ApplicationUserId)
                .NotEmpty().WithMessage("Customer has to link to an ApplicationUser");

            RuleFor(x => x.MembershipTierId)
                .GreaterThan(0)
                .When(x => x.MembershipTierId.HasValue)
                .WithMessage("Tier is invalid");
        }
    }
}
