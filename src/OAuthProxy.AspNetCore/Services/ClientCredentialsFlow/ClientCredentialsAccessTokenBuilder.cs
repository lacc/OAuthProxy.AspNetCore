using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    internal class ClientCredentialsAccessTokenBuilder : IAccessTokenBuilder
    {
        private readonly ITokenStorageService _tokenService;
        private readonly AuthorizationFlowServiceFactory _authorizationFlowServiceFactory;
        private readonly IOptionsSnapshot<ThirdPartyProviderConfig> _options;
        private readonly ILogger<ClientCredentialsAccessTokenBuilder> _logger;

        public const string NoRefreshTokenValue = "";
        public ClientCredentialsAccessTokenBuilder(ITokenStorageService tokenService, 
            AuthorizationFlowServiceFactory authorizationFlowServiceFactory, IOptionsSnapshot<ThirdPartyProviderConfig> options, ILogger<ClientCredentialsAccessTokenBuilder> logger)
        {
            _tokenService = tokenService;
            _authorizationFlowServiceFactory = authorizationFlowServiceFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<AccessTokenBuilderResponse> BuildAccessTokenAsync(HttpRequestMessage request, string userId, string serviceName)
        {
            var token = await _tokenService.GetTokenAsync(userId, serviceName);
            if (token != null && !string.IsNullOrEmpty(token?.AccessToken))
            {
                if (token.IsExpired)
                {
                    _logger.LogWarning("Access token is expired for user {UserId} and service {ServiceName}.", userId, serviceName);
                    return new AccessTokenBuilderResponse
                    {
                        ErrorMessage = "Access token is expired.",
                        StatusCode = System.Net.HttpStatusCode.Unauthorized
                    };
                }

                return new AccessTokenBuilderResponse
                {
                    AccessToken = token.AccessToken,
                };
            }

            var clientCredentialsExchanger = _authorizationFlowServiceFactory.GetClientCredentialsTokenExchanger(serviceName);
            if (clientCredentialsExchanger == null)
            {
                _logger.LogWarning("Access token is not available for user {UserId} and service {ServiceName}.", userId, serviceName);

                return new AccessTokenBuilderResponse
                {
                    ErrorMessage = "Access token is not available.",
                    StatusCode = System.Net.HttpStatusCode.Unauthorized
                };
            }

            try
            {
                var providerConfig = _options.Get(serviceName);
                if (providerConfig?.OAuthConfiguration == null)
                {
                    _logger.LogError("Configuration for service '{ServiceName}' not found.", serviceName);
                    throw new InvalidOperationException($"Configuration for service '{serviceName}' not found.");
                }

                var response = await clientCredentialsExchanger.ExchangeTokenAsync(providerConfig.OAuthConfiguration);
                await _tokenService.SaveTokenAsync(userId, serviceName, response.AccessToken, NoRefreshTokenValue, response.ExpiresAt);

                return new AccessTokenBuilderResponse
                {
                    AccessToken = response.AccessToken,
                    StatusCode = string.IsNullOrEmpty(response.AccessToken) ?
                        System.Net.HttpStatusCode.Unauthorized :
                        System.Net.HttpStatusCode.OK,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging client credentials for user {UserId} and service {ServiceName}.", userId, serviceName);
                return new AccessTokenBuilderResponse
                {
                    ErrorMessage = "Error exchanging client credentials.",
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
