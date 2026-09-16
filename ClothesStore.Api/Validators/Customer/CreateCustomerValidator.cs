using ClothesStore.Api.DTOs.Customer;
using FluentValidation;

namespace ClothesStore.Api.Validators.Customer
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Họ không được để trống")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Tên không được để trống")
                .MaximumLength(100);

            RuleFor(x => x.ApplicationUserId)
                .NotEmpty().WithMessage("Phải liên kết với một tài khoản người dùng");

            RuleFor(x => x.MembershipTierId)
                .GreaterThan(0)
                .When(x => x.MembershipTierId.HasValue)
                .WithMessage("Hạng thành viên không hợp lệ");
        }
    }
}
