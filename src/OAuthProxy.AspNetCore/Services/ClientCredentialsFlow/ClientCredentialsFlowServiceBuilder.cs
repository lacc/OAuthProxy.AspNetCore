using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    public class ClientCredentialsFlowServiceBuilder : IBuilder
    {
        private readonly string _serviceProviderName;
        private readonly IServiceCollection _services;
        private Action<IServiceCollection>? _tokenExchangerBuilder;

        public ClientCredentialsFlowServiceBuilder(string serviceProviderName, IServiceCollection services)
        {
            _serviceProviderName = serviceProviderName;
            _services = services;   

            ConfigureDefaultServices();
        }

        private ClientCredentialsFlowServiceBuilder ConfigureDefaultServices()
        {
            if (string.IsNullOrWhiteSpace(_serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(_serviceProviderName));
            }
            _tokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IClientCredentialsTokenExchanger, ClientCredentialsFlowExchangeToken>(_serviceProviderName);
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

            _services.AddKeyedScoped<IAccessTokenBuilder, AuthorizationCodeFlowAccessTokenBuilder>(_serviceProviderName);
        }
    }
}
