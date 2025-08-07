using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.ClientCredentialsFlow;
using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Apis
{
    public record struct AuthorizeInput(string ClientId, string ClientSecret, string RedirectUri, string Scope);
    internal class OAuthClientCredentialsFlowApiMapper : IProxyApiMapper
    {
        private readonly ThirdPartyProviderConfig _providerConfig;
        private readonly OAuthProxyConfiguration _proxyConfiguration;
        private readonly ILogger<OAuthAuthorizationCodeFlowApiMapper> _logger;

        public OAuthClientCredentialsFlowApiMapper([ServiceKey] string serviceKey, 
            IOptionsSnapshot<ThirdPartyProviderConfig> options,
            OAuthProxyConfiguration proxyConfiguration,
            ILogger<OAuthAuthorizationCodeFlowApiMapper> logger)
        {
            _providerConfig = options.Get(serviceKey);
            if (_providerConfig == null)
            {
                throw new InvalidOperationException($"Configuration for service '{serviceKey}' not found.");
            }

            _proxyConfiguration = proxyConfiguration;
            _logger = logger;
        }

        public string ServiceProviderName => _providerConfig?.ServiceProviderName ?? 
            throw new InvalidOperationException("Service provider name is not configured.");
        private ThirdPartyServiceConfig OAuthConfiguration => _providerConfig?.OAuthConfiguration ?? 
            throw new InvalidOperationException("OAuth configuration is not set for the service provider.");
        
        public RouteGroupBuilder MapProxyEndpoints(RouteGroupBuilder app)
        {
            string serviceName = ServiceProviderName;
            
            var authUrl = "authorize";
            //var callbackUrl = "callback";

            var authApi = app.MapGet(authUrl, HandleAuthorize)
                .WithDisplayName($"OAuth Proxy API for {serviceName}")
                .WithDescription($"API endpoints for OAuth Proxy service: {serviceName}")
                .WithName($"OAuthProxyAuthorizeApi_{serviceName}")
                .RequireAuthorization();
                //.AllowAnonymous();

            //var callbackApi = app.MapGet(callbackUrl, CallbackHandler)
            //    .WithDisplayName($"OAuth Proxy Callback API for {serviceName}")
            //    .WithDescription($"Callback endpoint for OAuth Proxy service: {serviceName}")
            //    .WithName($"OAuthProxyCallbackApi_{serviceName}")
            //    .AllowAnonymous();

            return app;
        }
        private async Task<Results<Ok<string>, RedirectHttpResult, UnauthorizedHttpResult, BadRequest<string>>> HandleAuthorize(
            [FromHeader(Name = "SRX-Client-Id")] string clientId,
            [FromHeader(Name = "SRX-Api-Key")] string clientSecret,
            [FromQuery(Name = "scope")] string scope,
            HttpRequest httpRequest, ClientCredentialsFlowStorage flowStorage, IUserIdProvider userIdProvider, ITokenStorageService tokenStorage, AuthorizationFlowServiceFactory serviceFactory)
        {
            var userId = userIdProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is not provided in the request headers.");
                return TypedResults.Unauthorized();
            }
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("Client ID or Client Secret is missing in the request headers.");
                return TypedResults.BadRequest("Client ID and Client Secret are required.");
            }

            if (await flowStorage.CreateOrReCreate(ServiceProviderName, userId, clientId, clientSecret, scope))
            {
                _logger.LogError("Failed to create or recreate flow for service provider: {ServiceProviderName}", ServiceProviderName);
                return TypedResults.BadRequest("Failed to create or recreate flow.");
            }

            var tokenExchanger = serviceFactory.GetClientCredentialsTokenExchanger(ServiceProviderName);
            var token = await tokenExchanger.ExchangeTokenAsync(OAuthConfiguration, clientId,clientSecret, scope);

            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                _logger.LogError("Failed to exchange authorization code for access token. ");
                return TypedResults.BadRequest("Failed to exchange authorization code for access token.");
            }

            await tokenStorage.SaveTokenAsync(userId, ServiceProviderName,
                token.AccessToken, token.RefreshToken ?? string.Empty, token.ExpiresAt);

            var localRedirectUri = httpRequest.Query.ContainsKey(_proxyConfiguration.ApiMapperConfiguration.AuthorizeRedirectUrlParameterName) ?
                httpRequest.Query[_proxyConfiguration.ApiMapperConfiguration.AuthorizeRedirectUrlParameterName].ToString() :
                string.Empty;

            if (!IsValidRedirectUri(localRedirectUri))
            {
                _logger.LogWarning("Invalid redirect URI: {RedirectUri}, returning OK", localRedirectUri);
                return TypedResults.Ok("Success");
                
            }

            if (httpRequest.Headers.TryGetValue("X-Requested-With", out Microsoft.Extensions.Primitives.StringValues value) &&
                value == "XMLHttpRequest")
            {
                // If the request is an AJAX request, return the URL instead of redirecting
                _logger.LogInformation("Returning URL for AJAX request: {localRedirectUri}", localRedirectUri);
                return TypedResults.Ok(localRedirectUri);
            }

            // For non-AJAX requests, perform a redirect
            _logger.LogInformation("Redirecting to localRedirectUri URL: {localRedirectUri}", localRedirectUri);
            if(httpRequest.HttpContext?.Response == null)
            {
                _logger.LogError("HttpContext is null, cannot set response status code or headers. returning OK with url");
                return TypedResults.Ok(localRedirectUri);
            }

            _logger.LogInformation("Redirecting to local redirect URI: {RedirectUri}", localRedirectUri);
            return TypedResults.Redirect(localRedirectUri, true, true); // Redirect with permanent status code
        }


        private bool IsValidRedirectUri(string? redirectUri)
        {
            return !string.IsNullOrEmpty(redirectUri) &&
                   Uri.TryCreate(redirectUri, UriKind.Absolute, out var _uri) &&
                    (_uri.Scheme == Uri.UriSchemeHttps ||
                   (_providerConfig.AllowHttpRedirects && _uri.Scheme == Uri.UriSchemeHttp)) &&
                   IsUrlWhitelisted(
                       _proxyConfiguration.ApiMapperConfiguration.WhitelistedRedirectUrls, redirectUri);
        }

        private static bool IsUrlWhitelisted(IEnumerable<string> whitelistedRedirectUrls, string redirectUrl) => !whitelistedRedirectUrls.Any() ||
                whitelistedRedirectUrls.Any(url =>
                    url.Equals(redirectUrl, StringComparison.OrdinalIgnoreCase) ||
                    (url.EndsWith('*') && redirectUrl.StartsWith(url.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)));
    }
}
