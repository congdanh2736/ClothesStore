// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho khuyến mãi (trước đây chưa có)
using ClothesStore.Api.DTOs.Promotion;
using FluentValidation;

namespace ClothesStore.Api.Validators.Promotion
{
    public class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
    {
        public CreatePromotionRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã khuyến mãi là bắt buộc.")
                .MaximumLength(50).WithMessage("Mã khuyến mãi không quá 50 ký tự.");
            RuleFor(x => x.DiscountValue).GreaterThan(0).WithMessage("Giá trị giảm phải lớn hơn 0.");
            RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }

    public class UpdatePromotionRequestValidator : AbstractValidator<UpdatePromotionRequest>
    {
        public UpdatePromotionRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã khuyến mãi là bắt buộc.")
                .MaximumLength(50).WithMessage("Mã khuyến mãi không quá 50 ký tự.");
            RuleFor(x => x.DiscountValue).GreaterThan(0).WithMessage("Giá trị giảm phải lớn hơn 0.");
            RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }
}
