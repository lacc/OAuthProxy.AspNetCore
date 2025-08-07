using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IClientCredentialsTokenExchanger
    {
        Task<TokenExchangeResponse> ExchangeTokenAsync(ThirdPartyServiceConfig config, string clientId, string clientSecret, string scope);
    }
}
