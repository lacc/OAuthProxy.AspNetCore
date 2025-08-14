using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface ISecretProvider
    {
        Task<ThirdPartySecrets> GetSecretsAsync(ThirdPartyServiceConfig config);
    }
}
