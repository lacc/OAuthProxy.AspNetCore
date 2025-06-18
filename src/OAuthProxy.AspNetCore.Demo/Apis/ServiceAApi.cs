using Microsoft.Extensions.DependencyInjection;

namespace OAuthProxy.AspNetCore.Demo.Apis
{
    public static class ServiceAApi
    {
        public static IEndpointRouteBuilder MapServiceAClientEndpoints(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("ServiceA");
     
            api.MapGet("proxy_endpoint", async (HttpRequest request,
                [FromKeyedServices("ServiceA")] HttpClient httpClient) =>
            {
                var relativeUrl = "some_thirdparty_endpoint";
                var response = await httpClient.GetAsync(relativeUrl);
                var content = await response.Content.ReadAsStringAsync();
                return Results.Ok(content);
            });

            return app;
        }
    }
}