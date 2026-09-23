using ClothesStore.Api.DTOs.StoreDailyStat;
using ClothesStore.Api.Interface.Services.Base;

namespace ClothesStore.Api.Interface.Services
{
    public interface IStoreDailyStatService : IService<StoreDailyStatDto, CreateStoreDailyStatDto, UpdateStoreDailyStatDto>
    {
    }
}
