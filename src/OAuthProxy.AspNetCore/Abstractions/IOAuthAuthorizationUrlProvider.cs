using OAuthProxy.AspNetCore.Configurations;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IOAuthAuthorizationUrlProvider
    {
        Task<string> GetAuthorizeUrlAsync(ThirdPartyServiceConfig config, string redirectUri);
    }
}
