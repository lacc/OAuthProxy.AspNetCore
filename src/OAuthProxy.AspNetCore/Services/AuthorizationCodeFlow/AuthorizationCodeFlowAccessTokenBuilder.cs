using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow
{
    internal class AuthorizationCodeFlowAccessTokenBuilder : IAccessTokenBuilder
    {
        private readonly ITokenStorageService _tokenService;
        private readonly ILogger<AuthorizationCodeFlowAccessTokenBuilder> _logger;

        public AuthorizationCodeFlowAccessTokenBuilder(ITokenStorageService tokenService, ILogger<AuthorizationCodeFlowAccessTokenBuilder> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
            // Initialization logic can go here if needed
        }

        public async Task<AccessTokenBuilderResponse> BuildAccessTokenAsync(HttpRequestMessage request, string userId, string serviceName)
        {
            var token = await _tokenService.GetTokenAsync(userId, serviceName);
            if (token == null || string.IsNullOrEmpty(token?.AccessToken))
            {
                _logger.LogWarning("Access token is not available for user {UserId} and service {ServiceName}.", userId, serviceName);
                return new AccessTokenBuilderResponse
                {
                    ErrorMessage = "Access token is not available.",
                    StatusCode = System.Net.HttpStatusCode.Unauthorized
                };
            }

            if (token.IsExpired)
            {
                if (string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogWarning("Access token is expired and no refresh token is available for user {UserId} and service {ServiceName}.", userId, serviceName);
                    return new AccessTokenBuilderResponse
                    {
                        ErrorMessage = "Access token is expired and no refresh token is available.",
                        StatusCode = System.Net.HttpStatusCode.Unauthorized
                    };
                }

                // If the token is expired but a refresh token is available, we can attempt to refresh it
                try
                {
                    var refreshedToken = await _tokenService.RefreshTokenAsync(userId, serviceName, token.RefreshToken);
                    if (string.IsNullOrEmpty(refreshedToken?.AccessToken))
                    {
                        _logger.LogWarning("Failed to refresh access token for user {UserId} and service {ServiceName}.", userId, serviceName);
                        return new AccessTokenBuilderResponse
                        {
                            ErrorMessage = "Failed to refresh access token.",
                            StatusCode = System.Net.HttpStatusCode.Unauthorized
                        };
                    }

                    _logger.LogInformation("Access token refreshed successfully for user {UserId} and service {ServiceName}.", userId, serviceName);
                    token = refreshedToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing access token for user {UserId} and service {ServiceName}.", userId, serviceName);
                    return new AccessTokenBuilderResponse
                    {
                        ErrorMessage = "An internal server error occurred while refreshing the access token.",
                        StatusCode = System.Net.HttpStatusCode.InternalServerError
                    };
                }
            }

            return new AccessTokenBuilderResponse
            {
                AccessToken = token.AccessToken
            };
        }
    }
}
