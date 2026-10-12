// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho giao dịch thanh toán (trước đây chưa có)
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.PaymentTransaction;
using FluentValidation;

namespace ClothesStore.Api.Validators.PaymentTransaction
{
    public class CreatePaymentTransactionRequestValidator : AbstractValidator<CreatePaymentTransactionRequest>
    {
        public CreatePaymentTransactionRequestValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Mã đơn hàng không hợp lệ.");
            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Phương thức thanh toán là bắt buộc.")
                .Must(m => PaymentMethods.All.Contains(m.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Phương thức thanh toán phải là một trong: {string.Join(", ", PaymentMethods.All)}.");
        }
    }

    public class UpdatePaymentTransactionRequestValidator : AbstractValidator<UpdatePaymentTransactionRequest>
    {
        public UpdatePaymentTransactionRequestValidator()
        {
            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Phương thức thanh toán là bắt buộc.")
                .Must(m => PaymentMethods.All.Contains(m.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Phương thức thanh toán phải là một trong: {string.Join(", ", PaymentMethods.All)}.");
        }
    }

    public class UpdatePaymentStatusRequestValidator : AbstractValidator<UpdatePaymentStatusRequest>
    {
        public UpdatePaymentStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => s == PaymentStatus.Paid || s == PaymentStatus.Failed || s == PaymentStatus.Pending)
                .WithMessage("Trạng thái thanh toán chỉ được là Paid, Failed hoặc Pending.");
            RuleFor(x => x.TransactionCode).MaximumLength(100);
        }
    }
}
