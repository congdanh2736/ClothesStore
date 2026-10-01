using ClothesStore.Api.DTOs.Promotion;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services;

public interface IPromotionService : IService<PromotionDto, CreatePromotionRequest, UpdatePromotionRequest>
{
}
