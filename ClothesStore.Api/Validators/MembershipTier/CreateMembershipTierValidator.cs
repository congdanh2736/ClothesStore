using ClothesStore.Api.DTOs.MembershipTier;
using FluentValidation;

namespace ClothesStore.Api.Validators.MembershipTier
{
    public class CreateMembershipTierValidator : AbstractValidator<CreateMembershipTierDto>
    {
        public CreateMembershipTierValidator()
        {
            RuleFor(x => x.TierName)
                .NotEmpty().WithMessage("Tier name is required.")
                .MaximumLength(50).WithMessage("Tier name must not exceed 50 characters.");
        }
    }
}
