using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IRefreshTokenService
    {
        Task<TokenResponse?> RefreshTokenAsync(string serviceName, string refreshToken);
    }
}