// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho vận chuyển
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.Shipment;
using FluentValidation;

namespace ClothesStore.Api.Validators.Shipment
{
    public class CreateShipmentValidator : AbstractValidator<CreateShipmentDto>
    {
        public CreateShipmentValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Mã đơn hàng không hợp lệ.");
            RuleFor(x => x.Carrier)
                .NotEmpty().WithMessage("Đơn vị vận chuyển là bắt buộc.")
                .MaximumLength(100);
            RuleFor(x => x.TrackingNumber).MaximumLength(100);
        }
    }

    public class UpdateShipmentValidator : AbstractValidator<UpdateShipmentDto>
    {
        public UpdateShipmentValidator()
        {
            RuleFor(x => x.Carrier)
                .NotEmpty().WithMessage("Đơn vị vận chuyển là bắt buộc.")
                .MaximumLength(100);
            RuleFor(x => x.TrackingNumber).MaximumLength(100);
        }
    }

    public class UpdateShipmentStatusValidator : AbstractValidator<UpdateShipmentStatusDto>
    {
        public UpdateShipmentStatusValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => ShipmentStatus.All.Contains(s))
                .WithMessage($"Trạng thái vận chuyển phải là một trong: {string.Join(", ", ShipmentStatus.All)}.");
        }
    }
}
