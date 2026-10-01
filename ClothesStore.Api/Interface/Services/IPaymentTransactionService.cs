using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services;

public interface IPaymentTransactionService : IService<PaymentTransactionDto, CreatePaymentTransactionRequest, UpdatePaymentTransactionRequest>
{
}
