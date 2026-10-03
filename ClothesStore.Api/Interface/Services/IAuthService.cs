using ClothesStore.Api.DTOs.Auth;

namespace ClothesStore.Api.Interface.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, RegisterResultDto? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? Error, LoginResultDto? Data)> LoginAsync(LoginDto dto);
    }
}
