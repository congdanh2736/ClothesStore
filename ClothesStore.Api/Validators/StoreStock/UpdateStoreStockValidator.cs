using ClothesStore.Api.DTOs.StoreStock;
using FluentValidation;

namespace ClothesStore.Api.Validators.StoreStock
{
    public class UpdateStoreStockValidator : AbstractValidator<UpdateStoreStockDto>
    {
        public UpdateStoreStockValidator()
        {
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        }
    }
}
