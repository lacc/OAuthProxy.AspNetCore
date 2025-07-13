using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Apis
{
    internal class OAuthAuthorizationCodeFlowApiMapper : IProxyApiMapper
    {
        private readonly ThirdPartyProviderConfig _providerConfig;
        private readonly OAuthProxyConfiguration _proxyConfiguration;
        private readonly ILogger<OAuthAuthorizationCodeFlowApiMapper> _logger;

        public OAuthAuthorizationCodeFlowApiMapper([ServiceKey] string serviceKey, 
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
            var callbackUrl = "callback";

            var authApi = app.MapGet(authUrl, HandleAuthorize)
                .WithDisplayName($"OAuth Proxy API for {serviceName}")
                .WithDescription($"API endpoints for OAuth Proxy service: {serviceName}")
                .WithName($"OAuthProxyAuthorizeApi_{serviceName}")
                .RequireAuthorization();
                //.AllowAnonymous();

            var callbackApi = app.MapGet(callbackUrl, CallbackHandler)
                .WithDisplayName($"OAuth Proxy Callback API for {serviceName}")
                .WithDescription($"Callback endpoint for OAuth Proxy service: {serviceName}")
                .WithName($"OAuthProxyCallbackApi_{serviceName}")
                .AllowAnonymous();

            return app;
        }
        private async Task<Results<Ok<string>, RedirectHttpResult, UnauthorizedHttpResult, BadRequest<string>>> HandleAuthorize(
            HttpRequest httpRequest, AuthorizationFlowServiceFactory serviceFactory, IAuthorizationStateService stateService)
        {
            var urlProvider = serviceFactory.GetAuthorizationUrlProvider(ServiceProviderName);
            if (urlProvider == null)
            {
                _logger.LogError("No authorization URL provider registered for service '{ServiceProviderName}'", ServiceProviderName);
                return TypedResults.BadRequest("Service configuration error");
            }

            var redirectUri = httpRequest.GetDisplayUrl().Replace("authorize", "callback");
            if (!string.IsNullOrEmpty(httpRequest.QueryString.Value))
            {
                redirectUri = redirectUri.Replace(httpRequest.QueryString.Value, "");
            }
        
            var localRedirectUri = httpRequest.Query.ContainsKey(_proxyConfiguration.ApiMapperConfiguration.AuthorizeRedirectUrlParameterName) ?
                httpRequest.Query[_proxyConfiguration.ApiMapperConfiguration.AuthorizeRedirectUrlParameterName].ToString() :
                string.Empty;

            var authorizeUrl = await urlProvider.GetAuthorizeUrlAsync(OAuthConfiguration, redirectUri);
            authorizeUrl = await stateService.DecorateWithStateAsync(ServiceProviderName, authorizeUrl, new AuthorizationStateParameters
            {
                 RedirectUrl = localRedirectUri
            });
            
            if (httpRequest.Headers.TryGetValue("X-Requested-With", out Microsoft.Extensions.Primitives.StringValues value) &&
                value == "XMLHttpRequest")
            {
                // If the request is an AJAX request, return the URL instead of redirecting
                _logger.LogInformation("Returning authorization URL for AJAX request: {AuthorizeUrl}", authorizeUrl);
                return TypedResults.Ok(authorizeUrl);
            }

            // For non-AJAX requests, perform a redirect
            _logger.LogInformation("Redirecting to authorization URL: {authorizeUrl}", authorizeUrl);
            if(httpRequest.HttpContext?.Response == null)
            {
                _logger.LogError("HttpContext is null, cannot set response status code or headers. returning OK with url");
                return TypedResults.Ok(authorizeUrl);
            }

            //httpRequest.HttpContext.Response.Redirect(authorizeUrl, false);
            //httpRequest.HttpContext.Response.StatusCode = StatusCodes.Status302Found;
            //httpRequest.HttpContext.Response.Headers.Append("Location", authorizeUrl);
            
            return TypedResults.Redirect(authorizeUrl, false, true); // Redirect with temporary status code
        }

        private async Task<Results<Ok<string>, BadRequest<string>, UnauthorizedHttpResult, RedirectHttpResult>> CallbackHandler(
            string code, string state, HttpRequest request, AuthorizationFlowServiceFactory serviceFactory, IAuthorizationStateService stateService, ITokenStorageService tokenStorage)
        {
            var stateValidationResult = await stateService.ValidateStateAsync(ServiceProviderName, state);
            if (!stateValidationResult?.IsValid ?? false)
            {
                _logger.LogWarning("Invalid state parameter on callback");
                return TypedResults.Unauthorized();
            }

            var tokenExchanger = serviceFactory.GetAuthorizationTokenExchanger(ServiceProviderName);
            var token = await tokenExchanger.ExchangeCodeAsync(OAuthConfiguration, code);

            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                _logger.LogError("Failed to exchange authorization code for access token. ");
                return TypedResults.BadRequest("Failed to exchange authorization code for access token.");
            }

            var userId = stateValidationResult?.StateParameters?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is missing in the state parameters.");
                return TypedResults.Unauthorized();
            }

            await tokenStorage.SaveTokenAsync(userId, ServiceProviderName, 
                token.AccessToken, token.RefreshToken ?? string.Empty, token.ExpiresAt);

            var redirectUri = stateValidationResult?.StateParameters?.RedirectUrl ?? string.Empty;
            if (IsValidRedirectUri(redirectUri))
            {
                _logger.LogInformation("Redirecting to local redirect URI: {RedirectUri}", redirectUri);
                return TypedResults.Redirect(redirectUri, true, true); // Redirect with permanent status code
            }

            _logger.LogWarning("Invalid redirect URI: {RedirectUri}, returning OK", redirectUri);
            return TypedResults.Ok("Success");
        }

        private bool IsValidRedirectUri(string? redirectUri)
        {
            return !string.IsNullOrEmpty(redirectUri) &&
                   Uri.TryCreate(redirectUri, UriKind.Absolute, out var _uri) &&
                    (_uri.Scheme == Uri.UriSchemeHttps ||
                   (_providerConfig.AllowHttpRedirects && _uri.Scheme == Uri.UriSchemeHttp)) &&
                   EnsureWhiteListedUrl(
                       _proxyConfiguration.ApiMapperConfiguration.WhiteListedRedirectUrls, redirectUri);
        }

        private static bool EnsureWhiteListedUrl(IEnumerable<string> whiteListRedirectUrls, string redirectUrl)
        {
            return !whiteListRedirectUrls.Any() ||
                whiteListRedirectUrls.Any(url => 
                    url.Equals(redirectUrl, StringComparison.OrdinalIgnoreCase) ||
                    (url.EndsWith('*') && redirectUrl.StartsWith(url.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)));
        }
    }
}
