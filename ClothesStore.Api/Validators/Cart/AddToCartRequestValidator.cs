using FluentValidation;
using ClothesStore.DTOs.Cart;

namespace ClothesStore.Validators.Cart
{
    public class AddToCartRequestValidator : AbstractValidator<AddToCartRequestDto>
    {
        public AddToCartRequestValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Mã khách hàng không hợp lệ.");

            RuleFor(x => x.VariantId)
                .GreaterThan(0).WithMessage("Mã phiên bản sản phẩm (Variant) không hợp lệ.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng sản phẩm thêm vào phải lớn hơn 0.");
        }
    }
}