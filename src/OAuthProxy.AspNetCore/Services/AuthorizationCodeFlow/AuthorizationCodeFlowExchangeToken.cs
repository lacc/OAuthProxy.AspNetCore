using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System.Text.Json;

namespace OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow
{
    internal class AuthorizationCodeFlowExchangeToken : IOAuthAuthorizationTokenExchanger
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthorizationCodeFlowExchangeToken> _logger;

        public AuthorizationCodeFlowExchangeToken(HttpClient httpClient, ILogger<AuthorizationCodeFlowExchangeToken> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TokenExchangeResponse> ExchangeCodeAsync(ThirdPartyServiceConfig config, string code)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", config.ClientId },
                    { "client_secret", config.ClientSecret },
                    { "code", code }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            if (tokenResponse == null)
            {
                _logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to exchange code for access token.");
            }

            return new TokenExchangeResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken  = tokenResponse.RefreshToken ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };


        }
    }
}
