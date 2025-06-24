using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Tests.IntegrationTests
{
    public class DummyCodeExchanger : IOAuthAuthorizationTokenExchanger
    {
        public Task<TokenExchangeResponse> ExchangeCodeAsync(ThirdPartyServiceConfig config, string code)
        {
            var tempAccessToken = "access";
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
