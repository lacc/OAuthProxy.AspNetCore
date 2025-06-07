using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace OAuthProxy.AspNetCore.Services.ThirdPartyServices
{
    public abstract class BaseAuthorizationCodeFlowClient : IThirdPartyOAuthService
    {
        private readonly HttpClient _httpClient;
        protected readonly ThirdPartyServiceConfig Config;
        protected readonly ILogger Logger;

        public string ServiceName { get; }

        public BaseAuthorizationCodeFlowClient(string serviceProviderName, HttpClient httpClient, ThirdPartyServiceConfig serviceConfig, ILogger logger)
        {
            ServiceName = serviceProviderName ?? throw new ArgumentNullException(nameof(serviceProviderName));
            _httpClient = httpClient;
            Config = serviceConfig;
            Logger = logger;
        }

        public virtual async Task<string> GetAuthorizeUrlAsync(string redirectUri)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = Config.ClientId;
            query["redirect_uri"] = redirectUri;
            query["response_type"] = "code";
            query["scope"] = Config.Scopes;
            query["state"] = Guid.NewGuid().ToString();

            return await Task.FromResult( $"{Config.AuthorizeEndpoint}?{query}");
        }

        public virtual async Task<(string AccessToken, string RefreshToken, DateTime Expiry)> 
            ExchangeCodeAsync(string code, string redirectUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", Config.ClientId },
                    { "client_secret", Config.ClientSecret },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            if (tokenResponse == null)
            {
                Logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to exchange code for access token.");
            }

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty, DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
        }

        public virtual async Task<(string AccessToken, string RefreshToken, DateTime Expiry)> 
            RefreshAccessTokenAsync(string refreshToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Config.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", Config.ClientId },
                    { "client_secret", Config.ClientSecret },
                    { "refresh_token", refreshToken }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            if(tokenResponse == null)
            {
                Logger.LogError("Failed to deserialize token response: {Response}", json);
                throw new InvalidOperationException("Failed to refresh access token.");
            }

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty, DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
        }

        public virtual async Task<HttpResponseMessage> CallEndpointAsync(string endpoint, string accessToken, HttpMethod method, object? data = null)
        {
            var request = new HttpRequestMessage(method, $"{Config.ApiBaseUrl}/{endpoint}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            };
            if (data != null && method != HttpMethod.Get)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            }

            var res = await _httpClient.SendAsync(request);
            return res;
        }

    }
}
