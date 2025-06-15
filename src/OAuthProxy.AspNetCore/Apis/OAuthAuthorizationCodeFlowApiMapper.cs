using Azure.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Services;
using System.Web;

namespace OAuthProxy.AspNetCore.Apis
{
    internal class OAuthAuthorizationCodeFlowApiMapper : IProxyApiMapper
    {
        private readonly ThirdPartyProviderConfig _providerConfig;

        public OAuthAuthorizationCodeFlowApiMapper([ServiceKey] string serviceKey, IOptionsSnapshot<ThirdPartyProviderConfig> options)
        {
            _providerConfig = options.Get(serviceKey);
            if (_providerConfig == null)
            {
                throw new InvalidOperationException($"Configuration for service '{serviceKey}' not found.");
            }
        }

        public string ServiceProviderName => _providerConfig?.ServiceProviderName ?? 
            throw new InvalidOperationException("Service provider name is not configured.");
        private ThirdPartyServiceConfig OAuthConfiguration => _providerConfig?.OAuthConfiguration ?? 
            throw new InvalidOperationException("OAuth configuration is not set for the service provider.");

        public RouteGroupBuilder MapProxyEndpoints(RouteGroupBuilder app)
        {
            string serviceName = ServiceProviderName;
            
            var authUrl = $"authorize";
            var callbackUrl = $"callback";

            var authApi = app.MapGet(authUrl, HandleAuthorize)
                .WithDisplayName($"OAuth Proxy API for {serviceName}")
                .WithDescription($"API endpoints for OAuth Proxy service: {serviceName}")
                .WithName($"OAuthProxyAuthorizeApi_{serviceName}")
                .RequireAuthorization();

            var callbackApi = app.MapGet(callbackUrl, CallbackHandler)
                .WithDisplayName($"OAuth Proxy Callback API for {serviceName}")
                .WithDescription($"Callback endpoint for OAuth Proxy service: {serviceName}")
                .WithName($"OAuthProxyCallbackApi_{serviceName}")
                .AllowAnonymous();

            return app;
        }
        private async Task<Results<Ok<string>, RedirectHttpResult, UnauthorizedHttpResult>> HandleAuthorize(
            HttpRequest httpRequest, AuthorizationFlowServiceFactory serviceFactory, IAuthorizationStateService stateService)
        {
            var urlProvider = serviceFactory.GetAuthorizationUrlProvider(ServiceProviderName);
            var redirectUri = httpRequest.GetDisplayUrl().Replace("authorize", "callback");

            var authorizeUrl = await urlProvider.GetAuthorizeUrlAsync(OAuthConfiguration, redirectUri);
            authorizeUrl = await stateService.DecorateWithStateAsync(ServiceProviderName, authorizeUrl);

            if (httpRequest.Headers.TryGetValue("X-Requested-With", out Microsoft.Extensions.Primitives.StringValues value) &&
                value == "XMLHttpRequest")
            {
                // If the request is an AJAX request, return the URL instead of redirecting
                return TypedResults.Ok(authorizeUrl);
            }

            return TypedResults.Redirect(authorizeUrl, true, true); // Redirect with permanent status code
        }


        private async Task<Results<Ok<string>, BadRequest<string>, UnauthorizedHttpResult>> CallbackHandler(
            string code, string state, HttpRequest request, AuthorizationFlowServiceFactory serviceFactory, IAuthorizationStateService stateService, TokenStorageService tokenStorage)
        {
            stateService.EnsureValidState(ServiceProviderName, state);
            var stateValidationResult = await stateService.ValidateStateAsync(ServiceProviderName, state);
            if (!stateValidationResult?.IsValid ?? false)
            {
                return TypedResults.Unauthorized();
            }

            var tokenExchanger = serviceFactory.GetAuthorizationTokenExchanger(ServiceProviderName);
            var token = await tokenExchanger.ExchangeCodeAsync(OAuthConfiguration, code);

            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                return TypedResults.BadRequest("Failed to exchange authorization code for access token.");
            }

            var userId = stateValidationResult?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return TypedResults.Unauthorized();
            }

            await tokenStorage.SaveTokenAsync(userId, ServiceProviderName, 
                token.AccessToken, token.RefreshToken ?? string.Empty, token.ExpiresAt);

            return TypedResults.Ok("");
        }
    }
}
