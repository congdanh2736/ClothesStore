using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface ILoyaltyTransactionService : IService<LoyaltyTransactionDto, CreateLoyaltyTransactionDto, UpdateLoyaltyTransactionDto>
    {
    }
}
