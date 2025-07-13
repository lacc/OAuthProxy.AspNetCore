using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Services
{
    internal class RefreshTokenService : IRefreshTokenService
    {
        private readonly ILogger<RefreshTokenService> _logger;
        private readonly AuthorizationFlowServiceFactory _authorizationFlowServiceFactory;
        private readonly IOptionsSnapshot<ThirdPartyProviderConfig> _options;

        public RefreshTokenService(ILogger<RefreshTokenService> logger, AuthorizationFlowServiceFactory authorizationFlowServiceFactory, IOptionsSnapshot<ThirdPartyProviderConfig> options)
        {
            _logger = logger;
            _authorizationFlowServiceFactory = authorizationFlowServiceFactory;
            _options = options;
        }

        public async Task<TokenExchangeResponse?> RefreshTokenAsync(string serviceName, string refreshToken)
        {
            var providerConfig = _options.Get(serviceName);
            if (providerConfig?.OAuthConfiguration == null)
            {
                _logger.LogError("Configuration for service '{ServiceName}' not found.", serviceName);
                throw new InvalidOperationException($"Configuration for service '{serviceName}' not found.");
            }

            var tokenExchanger = _authorizationFlowServiceFactory.GetAuthorizationRefreshTokenExchanger(serviceName);

            try
            {
                var refreshedToken = await tokenExchanger.ExchangeRefreshTokenAsync(
                    providerConfig.OAuthConfiguration, refreshToken);

                return refreshedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for service '{ServiceName}'.", serviceName);
                return null;
            }
        }
    }
}
