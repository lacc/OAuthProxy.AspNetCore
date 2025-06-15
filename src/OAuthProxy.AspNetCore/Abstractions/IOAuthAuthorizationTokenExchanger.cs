using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IOAuthAuthorizationTokenExchanger
    {
        Task<TokenExchangeResponse> ExchangeCodeAsync(ThirdPartyServiceConfig config, string code);
    }
}