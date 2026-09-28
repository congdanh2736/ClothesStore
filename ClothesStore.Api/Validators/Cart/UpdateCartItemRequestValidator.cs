using FluentValidation;
using ClothesStore.Api.DTOs.Cart;

namespace ClothesStore.Api.Validators.Cart
{
    public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequestDto>
    {
        public UpdateCartItemRequestValidator()
        {
            RuleFor(x => x.CartItemId)
                .GreaterThan(0).WithMessage("Mã mục giỏ hàng không hợp lệ.");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Số lượng không được nhỏ hơn 0.");
        }
    }
}