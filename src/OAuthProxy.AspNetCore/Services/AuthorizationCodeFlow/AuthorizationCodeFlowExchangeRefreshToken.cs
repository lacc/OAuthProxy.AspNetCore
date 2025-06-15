using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System.Text.Json;

namespace OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow
{
    internal class AuthorizationCodeFlowExchangeRefreshToken : IOAuthAuthorizationRefreshTokenExchanger
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthorizationCodeFlowUrlProvider> _logger;

        public AuthorizationCodeFlowExchangeRefreshToken(HttpClient httpClient, ILogger<AuthorizationCodeFlowUrlProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TokenResponse> ExchangeRefreshTokenAsync(ThirdPartyServiceConfig config, string refresh_token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", config.ClientId },
                    { "client_secret", config.ClientSecret },
                    { "refresh_token", refresh_token }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            if (tokenResponse == null)
            {
                _logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to refresh access token.");
            }

            return new TokenResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                ExpiresIn = tokenResponse.ExpiresIn
            };


        }
    }
}
