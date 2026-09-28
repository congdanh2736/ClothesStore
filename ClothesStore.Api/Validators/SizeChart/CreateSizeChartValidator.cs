using ClothesStore.Api.DTOs.SizeChart;
using FluentValidation;

namespace ClothesStore.Api.Validators.SizeChart
{
    public class CreateSizeChartValidator : AbstractValidator<CreateSizeChartDto>
    {
        public CreateSizeChartValidator()
        {
            RuleFor(x => x.SizeLabel)
                .NotEmpty().WithMessage("Nhãn kích cỡ không được để trống")
                .MaximumLength(50).WithMessage("Nhãn kích cỡ không được vượt quá 50 ký tự");

            RuleFor(x => x.Measurements)
                .NotEmpty().WithMessage("Thông số kích cỡ không được để trống")
                .MaximumLength(200).WithMessage("Thông số kích cỡ không được vượt quá 200 ký tự");
        }
    }
}
