// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho CreateOrderRequest, CheckoutRequest, UpdateOrderRequest (trước đây Order chưa có validator)
using ClothesStore.Api.DTOs.Order;
using FluentValidation;

namespace ClothesStore.Api.Validators.Order
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Mã khách hàng không hợp lệ.");
            RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("Địa chỉ giao hàng không hợp lệ.");
            RuleFor(x => x.PromotionId).GreaterThan(0).When(x => x.PromotionId.HasValue)
                .WithMessage("Khuyến mãi không hợp lệ.");
            RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue)
                .WithMessage("Cửa hàng không hợp lệ.");
            RuleFor(x => x.PaymentMethod).MaximumLength(50);

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một sản phẩm.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.VariantId).GreaterThan(0).WithMessage("Phiên bản sản phẩm không hợp lệ.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
            });
        }
    }

    public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
    {
        public CheckoutRequestValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Mã khách hàng không hợp lệ.");
            RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("Địa chỉ giao hàng không hợp lệ.");
            RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue)
                .WithMessage("Cửa hàng không hợp lệ.");
            RuleFor(x => x.PromotionCode).MaximumLength(50);
            RuleFor(x => x.PaymentMethod).MaximumLength(50);
        }
    }

    public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
    {
        public UpdateOrderRequestValidator()
        {
            RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("Địa chỉ giao hàng không hợp lệ.");
            RuleFor(x => x.PromotionId).GreaterThan(0).When(x => x.PromotionId.HasValue)
                .WithMessage("Khuyến mãi không hợp lệ.");
            RuleFor(x => x.Items)
                .NotEmpty().When(x => x.Items != null)
                .WithMessage("Đơn hàng phải có ít nhất một sản phẩm.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.VariantId).GreaterThan(0).WithMessage("Phiên bản sản phẩm không hợp lệ.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
            });
        }
    }
}
