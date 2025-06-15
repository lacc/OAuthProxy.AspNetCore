using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Configurations
{
    public class ProxyClientBuilderOption<TClient>
    {
        public required string ServiceProviderName { get; set; }
        public ThirdPartyServiceConfig? OAuthConfiguration { get; set; }
        internal IBuilder? AuthorizationFlowBuilder { get; set; }

    }
}
