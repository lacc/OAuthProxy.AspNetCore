using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using System.Net.Http.Headers;

namespace OAuthProxy.AspNetCore.Handlers
{
    internal class BasicOAuthBearerTokenHandler: DelegatingHandler
    {
        private readonly ITokenStorageService _tokenService;
        private readonly IUserIdProvider _userIdProvider;
        private readonly IProxyRequestContext _proxyRequestContext;
        private readonly ILogger<BasicOAuthBearerTokenHandler> _logger;


        public BasicOAuthBearerTokenHandler(ITokenStorageService tokenService, IUserIdProvider userIdProvider, IProxyRequestContext proxyRequestContext, ILogger<BasicOAuthBearerTokenHandler> logger)
        {
            _tokenService = tokenService;
            _userIdProvider = userIdProvider;
            _proxyRequestContext = proxyRequestContext;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var userId = _userIdProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID missing; cannot authorize request.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("User is not authenticated.")
                };
            }

            var serviceName = _proxyRequestContext.GetServiceName();
            if (string.IsNullOrEmpty(serviceName))
            {
                _logger.LogWarning("Service name is not specified; cannot retrieve token.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Service name is not specified.")
                };
            }

            var token = await _tokenService.GetTokenAsync(userId, serviceName);
            if (string.IsNullOrEmpty(token?.AccessToken))
            {
                _logger.LogWarning("Access token is not available for user {UserId} and service {ServiceName}.", userId, serviceName);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Access token is not available.")
                };
            }

            if (token.IsExpired)
            {
                if (string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogWarning("Access token is expired and no refresh token is available for user {UserId} and service {ServiceName}.", userId, serviceName);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent("Access token is expired and no refresh token is available.")
                    };
                }

                // If the token is expired but a refresh token is available, we can attempt to refresh it
                try
                {
                    var refreshedToken = await _tokenService.RefreshTokenAsync(userId, serviceName, token.RefreshToken);
                    if (string.IsNullOrEmpty(refreshedToken?.AccessToken))
                    {
                        _logger.LogWarning("Failed to refresh access token for user {UserId} and service {ServiceName}.", userId, serviceName);
                        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                        {
                            Content = new StringContent("Failed to refresh access token.")
                        };
                    }
                     
                    _logger.LogInformation("Access token refreshed successfully for user {UserId} and service {ServiceName}.", userId, serviceName);
                    token = refreshedToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing access token for user {UserId} and service {ServiceName}.", userId, serviceName);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("An internal server error occurred while refreshing the access token.")
                    };
                }

            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }


}
