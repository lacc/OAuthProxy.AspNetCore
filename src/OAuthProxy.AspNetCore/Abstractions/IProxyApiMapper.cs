using Microsoft.AspNetCore.Routing;

namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IProxyApiMapper
    {
        string ServiceProviderName { get; }
        RouteGroupBuilder MapProxyEndpoints(RouteGroupBuilder app);
    }
}
