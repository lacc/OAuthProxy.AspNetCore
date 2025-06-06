using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Services.ThirdPartyServices;

namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class ThirdPartyClientA : BaseAuthorizationCodeFlowClient
    {
        public ThirdPartyClientA(string serviceProviderName, HttpClient httpClient, ThirdPartyServiceConfig serviceConfig, ILogger logger) : 
            base(serviceProviderName, httpClient, serviceConfig, logger)
        {
        }

        public override Task<(string AccessToken, string RefreshToken, DateTime Expiry)> ExchangeCodeAsync(string code, string redirectUri)
        {
            //return base.ExchangeCodeAsync(code, redirectUri);
            var tempAccessToken = Config.ClientSecret;
            return Task.FromResult((tempAccessToken, string.Empty, DateTime.UtcNow.AddYears(1)));
        }
    }
}
