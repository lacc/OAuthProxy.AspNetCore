using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OAuthProxy.AspNetCore.Extensions;

public static class OAuthStartupExtensions
{
    public static OAuthProxyServiceBuilder AddThirdPartyOAuthProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        string thirdPartyClientConfigKey = "ThirdPartyServices")
    {
        var builder = new OAuthProxyServiceBuilder(services, configuration);
        builder.ConfigureThirdPartyOAuthProxyServices(thirdPartyClientConfigKey);

        return builder;
    }
}
