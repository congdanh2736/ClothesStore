using ClothesStore.Api.DTOs.LoyaltyTransaction;
using FluentValidation;

namespace ClothesStore.Api.Validators.LoyaltyTransaction
{
    public class UpdateLoyaltyTransactionValidator : AbstractValidator<UpdateLoyaltyTransactionDto>
    {
        public UpdateLoyaltyTransactionValidator()
        {
            // Các luật xác thực cho các thuộc tính của UpdateLoyaltyTransactionDto khi cập nhật
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId là bắt buộc")
                .GreaterThan(0).WithMessage("CustomerId phải lớn hơn 0");
            RuleFor(x => x.TxnDate)
                .NotEmpty().WithMessage("TxnDate là bắt buộc")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("TxnDate không thể là trong tương lai");
            RuleFor(x => x.PointsChange)
                .NotEmpty().WithMessage("PointsChange là bắt buộc")
                .NotEqual(0).WithMessage("PointsChange không thể là zero");
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Lý do không thể vượt quá 500 ký tự");
        }
    }
}
