using ClothesStore.Api.DTOs.Employee;
using FluentValidation;

namespace ClothesStore.Api.Validators.Employee
{
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
    {
        public CreateEmployeeValidator()
        {
            // Kiểm tra các quy tắc xác thực cho các thuộc tính của CreateEmployeeDto
            // Kiểm tra họ
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Bắt buộc phải có họ")
                .MaximumLength(100);
            // Kiểm tra tên
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Bắt buộc phải có tên")
                .MaximumLength(100);
            // Kiểm tra StoreId có hợp lệ không
            RuleFor(x => x.StoreId)
                .GreaterThan(0)
                .When(x => x.StoreId.HasValue)
                .WithMessage("Cửa hàng không hợp lệ");
            // Kiểm tra email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống");
            // Kiểm tra password
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");
        }
    }
}
