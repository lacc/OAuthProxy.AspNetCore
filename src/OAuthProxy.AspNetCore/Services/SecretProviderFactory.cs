using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services
{
    public class SecretProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SecretProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISecretProvider CreateSecretProvider(string thirdPartyProviderName)
        {
            if (string.IsNullOrEmpty(thirdPartyProviderName))
            {
                throw new InvalidOperationException("Third party provider name must be specified.");
            }
            var provider = _serviceProvider.GetKeyedService<ISecretProvider>(thirdPartyProviderName);
            if (provider == null)
            {
                throw new InvalidOperationException($"Secrets provider of type {thirdPartyProviderName} could not be resolved.");
            }
            return provider;
        }
    }
}
