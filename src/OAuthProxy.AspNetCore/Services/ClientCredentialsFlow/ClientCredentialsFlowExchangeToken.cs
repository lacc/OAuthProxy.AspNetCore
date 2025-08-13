using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System.Text.Json;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    internal class ClientCredentialsFlowExchangeToken : IClientCredentialsTokenExchanger
    {
        private readonly TimeSpan _defaultTokenExpiration = TimeSpan.FromDays(360);

        private readonly HttpClient _httpClient;
        private readonly ILogger<ClientCredentialsFlowExchangeToken> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        public ClientCredentialsFlowExchangeToken(HttpClient httpClient, ILogger<ClientCredentialsFlowExchangeToken> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TokenExchangeResponse> ExchangeTokenAsync(ThirdPartyServiceConfig config)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", config.ClientId },
                    { "client_secret", config.ClientSecret },
                    { "scope", config.Scopes }
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OAuth token exchange failed. Status Code: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"OAuth token exchange failed with status code {response.StatusCode}: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions);

            if (tokenResponse == null)
            {
                _logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to exchange client credentials for access token.");
            }

            return new TokenExchangeResponse
            {
                AccessToken = tokenResponse.AccessToken,
                ExpiresAt = tokenResponse.ExpiresIn > 0 ? 
                    DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn) :
                    DateTime.UtcNow.Add(_defaultTokenExpiration)
            };
        }
    }
}
