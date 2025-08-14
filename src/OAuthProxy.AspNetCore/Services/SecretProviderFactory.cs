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
            if (string.IsNullOrWhiteSpace(thirdPartyProviderName))
            {
                throw new InvalidOperationException($"Parameter '{nameof(thirdPartyProviderName)}' cannot be null or empty..");
            }
            var provider = _serviceProvider.GetKeyedService<ISecretProvider>(thirdPartyProviderName);
            if (provider == null)
            {
                throw new InvalidOperationException($"Secret provider for service '{thirdPartyProviderName}' could not be resolved.");
            }
            return provider;
        }
    }
}
