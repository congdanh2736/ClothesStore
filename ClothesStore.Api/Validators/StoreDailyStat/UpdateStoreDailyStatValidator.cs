using ClothesStore.Api.DTOs.StoreDailyStat;
using FluentValidation;

namespace ClothesStore.Api.Validators.StoreDailyStat
{
    public class UpdateStoreDailyStatValidator : AbstractValidator<UpdateStoreDailyStatDto>
    {
        public UpdateStoreDailyStatValidator()
        {
            RuleFor(x => x.StatDate).NotEqual(default(DateOnly));
            RuleFor(x => x.TotalOrders).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CompletedOrders).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CanceledOrders).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalRevenue).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalItemsSold).GreaterThanOrEqualTo(0);
            RuleFor(x => x)
                .Must(x => x.CompletedOrders + x.CanceledOrders <= x.TotalOrders)
                .WithMessage("Tổng đơn hoàn thành và hủy không được vượt tổng đơn.");
        }
    }
}
