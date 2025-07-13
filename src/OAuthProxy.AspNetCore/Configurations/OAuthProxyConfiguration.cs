using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Configurations
{
    public class OAuthProxyConfiguration
    {
        public TokenStorageConfiguration TokenStorageConfiguration { get; set; } =new TokenStorageConfiguration();


        public string ThirdPartyClientConfigKey { get; set; } = "ThirdPartyClients";
        
        public int DefaultHttpClientTimeoutSeconds { get; set; } = 30;
        public Action<IServiceCollection>? UserIdProvider { get; internal set; }
        public ApiMapperConfiguration ApiMapperConfiguration { get; } = new();
        internal List<IProxyClientBuilder> ProxyClientBuilders { get; } = [];
    }
}
