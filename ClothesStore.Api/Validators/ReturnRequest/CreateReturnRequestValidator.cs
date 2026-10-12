// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho yêu cầu đổi/trả
using ClothesStore.Api.DTOs.ReturnRequest;
using FluentValidation;

namespace ClothesStore.Api.Validators.ReturnRequest
{
    public class CreateReturnRequestValidator : AbstractValidator<CreateReturnRequestDto>
    {
        public CreateReturnRequestValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Mã đơn hàng không hợp lệ.");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do đổi/trả.")
                .MaximumLength(500);
            RuleFor(x => x.Items).NotEmpty().WithMessage("Phải chọn ít nhất một sản phẩm cần trả.");
            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.OrderItemId).Distinct().Count() == items.Count)
                .When(x => x.Items != null)
                .WithMessage("Mỗi sản phẩm trong đơn chỉ được khai báo một lần.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.OrderItemId).GreaterThan(0).WithMessage("Sản phẩm trong đơn không hợp lệ.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng trả phải lớn hơn 0.");
                item.RuleFor(i => i.Condition).MaximumLength(200);
            });
        }
    }
}
