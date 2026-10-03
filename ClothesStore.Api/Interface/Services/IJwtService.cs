using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Services
{
    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
