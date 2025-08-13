using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Services;
using System.Net.Http.Headers;

namespace OAuthProxy.AspNetCore.Handlers
{
    internal class BasicOAuthBearerTokenHandler: DelegatingHandler
    {
        private readonly IUserIdProvider _userIdProvider;
        private readonly IProxyRequestContext _proxyRequestContext;
        private readonly AuthorizationFlowServiceFactory _authorizationFlowServiceFactory;
        private readonly ILogger<BasicOAuthBearerTokenHandler> _logger;

        public BasicOAuthBearerTokenHandler(IUserIdProvider userIdProvider, 
            IProxyRequestContext proxyRequestContext, AuthorizationFlowServiceFactory authorizationFlowServiceFactory, ILogger<BasicOAuthBearerTokenHandler> logger)
        {
            _userIdProvider = userIdProvider;
            _proxyRequestContext = proxyRequestContext;
            _authorizationFlowServiceFactory = authorizationFlowServiceFactory;
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

            var tokenBuilder = _authorizationFlowServiceFactory.GetAccessTokenBuilder(serviceName);
            if(tokenBuilder == null)
            {
                _logger.LogWarning("No access token builder found for service {ServiceName}.", serviceName);
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("No access token builder found for the specified service.")
                };
            }

            var token = await tokenBuilder.BuildAccessTokenAsync(request, userId, serviceName);
            if(string.IsNullOrEmpty(token.AccessToken))
            {
                var statusCode = token.StatusCode != System.Net.HttpStatusCode.OK ? 
                    token.StatusCode : System.Net.HttpStatusCode.Unauthorized;
                var content = string.IsNullOrEmpty(token.ErrorMessage) ? 
                    "Access token is not available." : token.ErrorMessage;
                
                _logger.LogWarning("Failed to retrieve access token for user {UserId} and service {ServiceName}: {ErrorMessage}",
                    userId, serviceName, content);

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content)
                };
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }


}
