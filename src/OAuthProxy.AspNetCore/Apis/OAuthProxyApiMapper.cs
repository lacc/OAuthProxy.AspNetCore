using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;

namespace OAuthProxy.AspNetCore.Apis
{
    public static class OAuthProxyApiMapper
    {
        public static IEndpointRouteBuilder MapProxyClientEndpoints(this IEndpointRouteBuilder app)
        {
            
            using var scope = app.ServiceProvider.CreateScope();
            var proxyConfiguration = scope.ServiceProvider.GetRequiredService<OAuthProxyConfiguration>();
            var providers = scope.ServiceProvider.GetServices<IRegisteredProxyProviders>();

            var api = app.MapGroup(proxyConfiguration.ApiMapperConfiguration.ProxyUrlPrefix);

            foreach (var provider in providers)
            {
                var providerApi = api.MapGroup($"{provider.ServiceProviderName.ToLower()}")
                    .WithTags($"Proxy API for {provider.ServiceProviderName}")
                    .WithName($"ProxyApi_{provider.ServiceProviderName}Endpoints")
                    .WithDisplayName($"Proxy APIs for {provider.ServiceProviderName}")
                    .WithDescription($"API endpoints for Proxy service: {provider.ServiceProviderName}");

                var mappers = scope.ServiceProvider.GetKeyedServices<IProxyApiMapper>(provider.ServiceProviderName);
                foreach (var mapper in mappers)
                {
                    // Map the proxy token endpoints for each service
                    mapper.MapProxyEndpoints(providerApi);
                }

                if (proxyConfiguration.ApiMapperConfiguration.MapGenericApi)
                {
                    MapGenericApi(providerApi, provider.ServiceProviderName);
                }
            }

            return api;
        }

        private static void MapGenericApi(RouteGroupBuilder providerApi, string serviceProviderName)
        {
            providerApi.MapGet("{endpoint}", 
                async Task<Results<Ok<string>, BadRequest<string>, UnauthorizedHttpResult>> 
                (string endpoint, HttpRequest request, IHttpClientFactory httpClientFactory) =>
                {
                    var httpClient = httpClientFactory.CreateClient(serviceProviderName);
    
                    var content = await httpClient.GetAsync(endpoint);
                    var res = await content.Content.ReadAsStringAsync();
                    return TypedResults.Ok(res);
                })
                .WithDisplayName($"Get {serviceProviderName} Endpoint")
                .WithDescription($"Get data from {serviceProviderName} endpoint.")
                .RequireAuthorization();
        }
    }
}
