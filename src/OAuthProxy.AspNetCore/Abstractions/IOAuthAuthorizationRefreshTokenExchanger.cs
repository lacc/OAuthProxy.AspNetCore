using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IOAuthAuthorizationRefreshTokenExchanger
    {
        Task<TokenResponse> ExchangeRefreshTokenAsync(ThirdPartyServiceConfig config, string refresh_token);
    }
}