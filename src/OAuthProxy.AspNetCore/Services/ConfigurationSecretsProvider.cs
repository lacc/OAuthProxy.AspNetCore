using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Services
{
    internal class ConfigurationSecretsProvider : ISecretProvider
    {
        public Task<ThirdPartySecrets> GetSecretsAsync(string key, ThirdPartyServiceConfig config)
        {
            return Task.FromResult(new ThirdPartySecrets
            {
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                ApiKey = config.ApiKey
            });
        }
    }
}
