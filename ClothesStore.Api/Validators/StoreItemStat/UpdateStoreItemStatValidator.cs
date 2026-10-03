using ClothesStore.Api.DTOs.StoreItemStat;
using FluentValidation;

namespace ClothesStore.Api.Validators.StoreItemStat
{
    public class UpdateStoreItemStatValidator : AbstractValidator<UpdateStoreItemStatDto>
    {
        public UpdateStoreItemStatValidator()
        {
            RuleFor(x => x.StatDate).NotEqual(default(DateOnly));
            RuleFor(x => x.QuantitySold).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalRevenue).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReturnQuantity).GreaterThanOrEqualTo(0);
        }
    }
}
