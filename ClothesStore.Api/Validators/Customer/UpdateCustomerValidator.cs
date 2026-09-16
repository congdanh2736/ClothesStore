using ClothesStore.Api.DTOs.Customer;
using FluentValidation;

namespace ClothesStore.Api.Validators.Customer
{
    public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
    {
        public UpdateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
