using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IRefreshTokenService
    {
        Task<TokenExchangeResponse?> RefreshTokenAsync(string serviceName, string refreshToken);
    }
}