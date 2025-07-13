using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IOAuthAuthorizationRefreshTokenExchanger
    {
        Task<TokenExchangeResponse> ExchangeRefreshTokenAsync(ThirdPartyServiceConfig config, string refreshToken);
    }
}