using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using OAuthProxy.AspNetCore.Services;
using System.Security.Claims;

namespace OAuthProxy.AspNetCore.Apis
{
    public static class ProxyApi
    {
        public static RouteGroupBuilder MapProxyEndpoints(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("/api/proxy/");
            
            api.MapGet("{serviceName}/{endpoint}", async Task<Results<Ok<string>, BadRequest<string>>> 
                (string serviceName, string endpoint, HttpRequest request, ThirdPartyServiceFactory serviceFactory, OAuthService oauthService) =>
            {
                var userId = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = OAuthApi.DefaultUserId;
                    //return TypedResults.BadRequest("User not authenticated.");
                }
                
                var service = serviceFactory.GetService(serviceName);
                if (service == null)
                {
                    return TypedResults.BadRequest($"Service {serviceName} not found.");
                }

                var accessToken = await oauthService.GetValidAccessTokenAsync(userId, serviceName);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return TypedResults.BadRequest("Access token not available.");
                }

                var response = await service.CallEndpointAsync(endpoint, accessToken, HttpMethod.Get, null);

                var content = await response.Content.ReadAsStringAsync();
                return TypedResults.Ok(content);

                //return new ContentResult
                //{
                //    Content = content,
                //    ContentType = response.Content.Headers.ContentType?.ToString(),
                //    StatusCode = (int)response.StatusCode
                //};
            });
            api.MapPost("{serviceName}/{endpoint}", async Task<Results<Ok<string>, BadRequest<string>>>
                (string serviceName, string endpoint, object body, HttpRequest request, ThirdPartyServiceFactory serviceFactory, OAuthService oauthService) =>
            {
                var userId = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = OAuthApi.DefaultUserId;
                    //return TypedResults.BadRequest("User not authenticated.");
                }

                var service = serviceFactory.GetService(serviceName);
                if (service == null)
                {
                    
                    return TypedResults.BadRequest($"Service {serviceName} not found.");
                }

                var accessToken = await oauthService.GetValidAccessTokenAsync(userId, serviceName);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return TypedResults.BadRequest("Access token not available.");
                }
                
                var response = await service.CallEndpointAsync(endpoint, accessToken, HttpMethod.Post, null);

                var content = await response.Content.ReadAsStringAsync();
                return TypedResults.Ok(content);

                //return new ContentResult
                //{
                //    Content = content,
                //    ContentType = response.Content.Headers.ContentType?.ToString(),
                //    StatusCode = (int)response.StatusCode
                //};
            });
            return api; 
        }
    }
}
