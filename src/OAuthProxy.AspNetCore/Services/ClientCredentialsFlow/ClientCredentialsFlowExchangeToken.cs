using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System.Text.Json;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    internal class ClientCredentialsFlowExchangeToken : IClientCredentialsTokenExchanger
    {
        /// <summary>
        /// Fallback token expiration used when the token response does not specify an expiration.
        /// 360 days was chosen to ensure long-lived tokens for services that do not provide expiration information.
        /// Configure through TokenExpirationInDays in client appsettings configuration.
        /// </summary>
        private const int _defaultTokenExpirationInDays = 360;
        private static readonly TimeSpan _defaultTokenExpiration = TimeSpan.FromDays(_defaultTokenExpirationInDays);

        private readonly HttpClient _httpClient;
        private readonly SecretProviderFactory _secretProviderFactory;
        private readonly IUserIdProvider _userIdProvider;
        private readonly ILogger<ClientCredentialsFlowExchangeToken> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        public ClientCredentialsFlowExchangeToken(HttpClient httpClient, SecretProviderFactory secretProviderFactory, IUserIdProvider userIdProvider, ILogger<ClientCredentialsFlowExchangeToken> logger)
        {
            _httpClient = httpClient;
            _secretProviderFactory = secretProviderFactory;
            _userIdProvider = userIdProvider;
            _logger = logger;
        }

        public async Task<TokenExchangeResponse> ExchangeTokenAsync(ThirdPartyServiceConfig config)
        {
            var userId = _userIdProvider.GetCurrentUserId();
            var key = $"{config.Name}:{userId}";
            var secretProvider = _secretProviderFactory.CreateProvider(config.Name);
            var secrets = await secretProvider.GetSecretsAsync(key, config);

            var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", secrets.ClientId },
                    { "client_secret", secrets.ClientSecret },
                    { "scope", config.Scopes }
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OAuth token exchange failed. Status Code: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"OAuth token exchange failed with status code {response.StatusCode}. See logs for details.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions);

            if (tokenResponse == null)
            {
                _logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to exchange client credentials for access token.");
            }

            TimeSpan tokenExpiration;
            if (tokenResponse.ExpiresIn > 0)
            {
                tokenExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
            }
            else if (config.TokenExpirationInDays.HasValue)
            {
                tokenExpiration = TimeSpan.FromDays(config.TokenExpirationInDays.Value);
            }
            else
            {
                tokenExpiration = _defaultTokenExpiration;
            }

            return new TokenExchangeResponse
            {
                AccessToken = tokenResponse.AccessToken,
                ExpiresAt = DateTime.UtcNow.Add(tokenExpiration)
            };
        }
    }
}
