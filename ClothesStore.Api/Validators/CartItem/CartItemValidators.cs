using ClothesStore.Api.DTOs.CartItem;
using FluentValidation;

namespace ClothesStore.Api.Validators.CartItem
{
    public class CreateCartItemValidator : AbstractValidator<CreateCartItemDto>
    {
        public CreateCartItemValidator()
        {
            RuleFor(x => x.CartId)
                .GreaterThan(0).WithMessage("Mã giỏ hàng không hợp lệ.");

            RuleFor(x => x.VariantId)
                .GreaterThan(0).WithMessage("Mã biến thể sản phẩm không hợp lệ.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        }
    }

    public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemDto>
    {
        public UpdateCartItemValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        }
    }
}
