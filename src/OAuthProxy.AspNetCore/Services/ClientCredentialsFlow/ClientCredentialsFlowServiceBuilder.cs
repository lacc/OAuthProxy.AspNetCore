using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    public class ClientCredentialsFlowServiceBuilder : IBuilder
    {
        private readonly string _serviceProviderName;
        private readonly IServiceCollection _services;
        private Action<IServiceCollection>? _tokenExchangerBuilder;

        public ClientCredentialsFlowServiceBuilder(string serviceProviderName, IServiceCollection services)
        {
            ConfigureDefaultServices(serviceProviderName);

            _serviceProviderName = serviceProviderName;
            _services = services;   

        }

        private ClientCredentialsFlowServiceBuilder ConfigureDefaultServices(string serviceProviderName)
        {
            if (string.IsNullOrWhiteSpace(serviceProviderName))
            {   
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(serviceProviderName));
            }
            _tokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IClientCredentialsTokenExchanger, ClientCredentialsFlowExchangeToken>(serviceProviderName);
            };
            return this;
        }

        public ClientCredentialsFlowServiceBuilder ConfigureTokenExchanger<TService>()
            where TService : class, IClientCredentialsTokenExchanger
        {
            _tokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IClientCredentialsTokenExchanger, TService>(_serviceProviderName);
            };
            return this;
        }

        public void Build()
        {
            _tokenExchangerBuilder?.Invoke(_services);

            _services.AddKeyedScoped<IAccessTokenBuilder, ClientCredentialsAccessTokenBuilder>(_serviceProviderName);
        }
    }
}
