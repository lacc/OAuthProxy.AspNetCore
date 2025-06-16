using OAuthProxy.AspNetCore.Abstractions;
using System.Net.Http.Headers;

namespace OAuthProxy.AspNetCore.Handlers
{
    internal class BasicOAuthBearerTokenHandler: DelegatingHandler
    {
        private readonly ITokenStorageService _tokenCache;
        private readonly IUserIdProvider _userIdProvider;
        private readonly IProxyRequestContext _proxyRequestContext;

        //private readonly ITokenService _tokenService;

        public BasicOAuthBearerTokenHandler(ITokenStorageService tokenCache, IUserIdProvider userIdProvider, IProxyRequestContext proxyRequestContext)//ITokenService tokenService)
        {
            _tokenCache = tokenCache;
            _userIdProvider = userIdProvider;
            _proxyRequestContext = proxyRequestContext;
            //_tokenService = tokenService;

        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var userId = _userIdProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                // If userId is not available, we cannot proceed with the request
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("User is not authenticated.")
                };
            }

            var serviceName = _proxyRequestContext.GetServiceName();
            if (string.IsNullOrEmpty(serviceName))
            {
                // If serviceName is not available, we cannot proceed with the request
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Service name is not specified.")
                };
            }
            var token = await _tokenCache.GetTokenAsync(userId, serviceName);
            
            if (token == null)
            {
                // If token is not available, we cannot proceed with the request
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Access token is not available.")
                };
            }

            if (token.IsExpired)
            {
                if (token.RefreshToken == null)
                {
                    // If the token is expired and no refresh token is available, we cannot proceed with the request
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent("Access token is expired and no refresh token is available.")
                    };
                }

                else
                {
                    // If the token is expired but a refresh token is available, we can attempt to refresh it
                    try
                    {
                        var refreshedToken = await _tokenCache.RefreshTokenAsync(userId, serviceName, token.RefreshToken);
                        if (refreshedToken != null)
                        {
                            token = refreshedToken;
                        }
                        else
                        {
                            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                            {
                                Content = new StringContent("Failed to refresh access token.")
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent($"Error refreshing access token: {ex.Message}")
                        };
                    }

                }
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }


}
