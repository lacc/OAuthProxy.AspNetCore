using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OAuthProxy.AspNetCore.Extensions;

public static class OAuthStartupExtensions
{
    public static IServiceCollection AddThirdPartyOAuthProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ThirdPartyOAuthProxyBuilder> builderAction)
    {
        var builder = new ThirdPartyOAuthProxyBuilder(services, configuration);
        builderAction?.Invoke(builder);
        builder.Build();

        return services;
    }
}
