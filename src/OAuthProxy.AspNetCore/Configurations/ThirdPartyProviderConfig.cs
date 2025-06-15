namespace OAuthProxy.AspNetCore.Configurations
{
    public class ThirdPartyProviderConfig
    {
        public string? ServiceProviderName { get; internal set; }
        public ThirdPartyServiceConfig? OAuthConfiguration { get; internal set; }
    }
}