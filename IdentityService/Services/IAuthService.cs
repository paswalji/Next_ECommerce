using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterModel model);
     
        Task<AuthResponse> LoginAsync(LoginModel model, string ipAddress);
        Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress);
    }
}
