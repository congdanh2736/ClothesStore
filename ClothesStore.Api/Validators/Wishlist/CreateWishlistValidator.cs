using ClothesStore.Api.DTOs.Wishlist;
using FluentValidation;

namespace ClothesStore.Api.Validators.Wishlist
{
    public class CreateWishlistValidator : AbstractValidator<CreateWishlistDto>
    {
        public CreateWishlistValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("CustomerId phải lớn hơn 0.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId phải lớn hơn 0.");
        }
    }
}
