using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Demo
{
    public class DummyCodeExchanger : IOAuthAuthorizationTokenExchanger
    {
        public Task<TokenExchangeResponse> ExchangeCodeAsync(ThirdPartyServiceConfig config, string code)
        {
            var tempAccessToken = config.ClientSecret;
            return Task.FromResult(
                new TokenExchangeResponse
                {
                    AccessToken = tempAccessToken,
                    RefreshToken = string.Empty,
                    ExpiresAt = DateTime.UtcNow.AddSeconds( 3600 ) // 1 hour in seconds
                });
        }
    }
}
