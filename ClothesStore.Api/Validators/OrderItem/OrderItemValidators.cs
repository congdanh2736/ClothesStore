// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho chi tiết đơn hàng (trước đây chưa có)
using ClothesStore.Api.DTOs.OrderItem;
using FluentValidation;

namespace ClothesStore.Api.Validators.OrderItem
{
    public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
    {
        public CreateOrderItemRequestValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Mã đơn hàng không hợp lệ.");
            RuleFor(x => x.VariantId).GreaterThan(0).WithMessage("Phiên bản sản phẩm không hợp lệ.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        }
    }

    public class UpdateOrderItemRequestValidator : AbstractValidator<UpdateOrderItemRequest>
    {
        public UpdateOrderItemRequestValidator()
        {
            RuleFor(x => x.VariantId).GreaterThan(0).WithMessage("Phiên bản sản phẩm không hợp lệ.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        }
    }
}
