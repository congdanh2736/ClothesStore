// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Validator cho nhập / xuất kho
using ClothesStore.Api.DTOs.StoreStock;
using FluentValidation;

namespace ClothesStore.Api.Validators.StoreStock
{
    public class AdjustStockValidator : AbstractValidator<AdjustStockDto>
    {
        public AdjustStockValidator()
        {
            RuleFor(x => x.StoreId).GreaterThan(0);
            RuleFor(x => x.VariantId).GreaterThan(0);
            RuleFor(x => x.QuantityChange).NotEqual(0).WithMessage("Số lượng điều chỉnh phải khác 0.");
        }
    }
}
