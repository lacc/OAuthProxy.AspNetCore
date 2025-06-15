using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using System.Web;

namespace OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow
{
    internal class AuthorizationCodeFlowUrlProvider : IOAuthAuthorizationUrlProvider
    {
        public AuthorizationCodeFlowUrlProvider()
        {
        }

        public Task<string> GetAuthorizeUrlAsync(ThirdPartyServiceConfig config, string redirectUri)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = config.ClientId;
            query["redirect_uri"] = redirectUri;
            query["response_type"] = "code";
            query["scope"] = config.Scopes;
            query["state"] = Guid.NewGuid().ToString();

            return Task.FromResult($"{config.AuthorizeEndpoint}?{query}");

        }
    }
}
