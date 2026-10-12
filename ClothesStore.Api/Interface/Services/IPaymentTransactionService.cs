using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services;

public interface IPaymentTransactionService : IService<PaymentTransactionDto, CreatePaymentTransactionRequest, UpdatePaymentTransactionRequest>
{
    // [THÊM] Ghi nhận kết quả thanh toán (Paid / Failed)
    Task<(bool Success, string? Error)> UpdateStatusAsync(int id, UpdatePaymentStatusRequest request);
}
