using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Services
{
    internal class RefreshTokenService : IRefreshTokenService
    {
        private readonly AuthorizationFlowServiceFactory _authorizationFlowServiceFactory;
        private readonly IOptionsSnapshot<ThirdPartyProviderConfig> _options;

        public RefreshTokenService(AuthorizationFlowServiceFactory authorizationFlowServiceFactory, IOptionsSnapshot<ThirdPartyProviderConfig> options)
        {
            _authorizationFlowServiceFactory = authorizationFlowServiceFactory;
            _options = options;
        }

        public async Task<TokenResponse?> RefreshTokenAsync(string serviceName, string refreshToken)
        {
            var providerConfig = _options.Get(serviceName);
            if (providerConfig == null)
            {
                throw new InvalidOperationException($"Configuration for service '{serviceName}' not found.");
            }

            if (providerConfig.OAuthConfiguration == null)
            {
                return null;
            }
            var tokenExchanger = _authorizationFlowServiceFactory.GetAuthorizationRefreshTokenExchanger(serviceName);
            var refreshedToken = await tokenExchanger.ExchangeRefreshTokenAsync(
                providerConfig.OAuthConfiguration, refreshToken);

            return refreshedToken;
        }
    }
}
