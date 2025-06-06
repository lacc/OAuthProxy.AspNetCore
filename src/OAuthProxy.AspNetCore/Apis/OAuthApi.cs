using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using OAuthProxy.AspNetCore.Services;
using System.Security.Claims;

namespace OAuthProxy.AspNetCore.Apis;

public static class OAuthApi
{
    public const string DefaultUserId = "123";
    public static RouteGroupBuilder MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/oauth");//.HasApiVersion(1.0);

        api.MapGet("{serviceName}/authorize", async (string serviceName, HttpRequest request, OAuthService oauthService) =>
        {
            var redirectUri = request.GetDisplayUrl().Replace("authorize", "callback");
            var authorizeUrl = await oauthService.GetAuthorizeUrlAsync(serviceName, redirectUri);
            return TypedResults.Ok(new { AuthorizeUrl = authorizeUrl });
        })
        .WithDisplayName("Authorization URL")
        .WithDescription("Get the authorization URL for a third-party service.")
        .WithName("GetAuthorizationUrl") ;
        

        api.MapGet("{serviceName}/callback", async Task<Results<Ok<string>, BadRequest<string>>> 
            (string serviceName, string code, HttpRequest request, OAuthService oauthService) =>
        {
            var userId = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = DefaultUserId;
                //return TypedResults.BadRequest("User not authenticated.");
            }

            var redirectUri = request.GetDisplayUrl();
            if (await oauthService.HandleCallbackAsync(userId, serviceName, code, redirectUri))
            {
                return TypedResults.Ok("Authorization successful.");
            }

            return TypedResults.BadRequest("Authorization failed. Invalid code or redirect URI.");
        });

        api.MapGet("{serviceName}/hasValidToken", async (string serviceName, HttpRequest request, OAuthService oauthService) =>
        {
            var userId = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = DefaultUserId;
                //return TypedResults.BadRequest("User not authenticated.");
            }

            var authorizeUrl = await oauthService.GetValidAccessTokenAsync(userId, serviceName);
            return TypedResults.Ok(!string.IsNullOrEmpty(authorizeUrl));
        });


        return api;
    }
}
