using AutoMapper;
using ClothesStore.Api.DTOs.LoyaltyTransaction;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class LoyaltyTransactionProfile : Profile
    {
        public LoyaltyTransactionProfile()
        {
            CreateMap<LoyaltyTransaction, LoyaltyTransactionDto>();
            CreateMap<CreateLoyaltyTransactionDto, LoyaltyTransaction>();
            CreateMap<UpdateLoyaltyTransactionDto, LoyaltyTransaction>();
        }
    }
}
