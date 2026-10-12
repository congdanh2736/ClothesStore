// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho đăng nhập (trước đây chỉ có RegisterValidator)
using ClothesStore.Api.DTOs.Auth;
using FluentValidation;

namespace ClothesStore.Api.Validators.Auth
{
    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc.");
        }
    }
}
