using ClothesStore.Api.DTOs.StoreStock;
using FluentValidation;

namespace ClothesStore.Api.Validators.StoreStock
{
    public class CreateStoreStockValidator : AbstractValidator<CreateStoreStockDto>
    {
        public CreateStoreStockValidator()
        {
            RuleFor(x => x.StoreId).GreaterThan(0);
            RuleFor(x => x.VariantId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        }
    }
}
