using ClothesStore.Api.DTOs.Store;
using FluentValidation;

namespace ClothesStore.Api.Validators.Store
{
    public class UpdateStoreValidator : AbstractValidator<UpdateStoreDto>
    {
        public UpdateStoreValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
                .MaximumLength(200);
        }
    }
}
