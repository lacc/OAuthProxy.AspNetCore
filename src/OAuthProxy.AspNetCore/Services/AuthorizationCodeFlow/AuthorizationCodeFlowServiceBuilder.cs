using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Apis;

namespace OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow
{
    public class AuthorizationCodeFlowServiceBuilder : IBuilder
    {
        public readonly string ServiceProviderName;
        private readonly IServiceCollection _services;

        private Action<IServiceCollection>? _urlProviderBuilder;
        private Action<IServiceCollection>? _tokenExchangerBuilder;
        private Action<IServiceCollection>? _refreshTokenExchangerBuilder;

        public AuthorizationCodeFlowServiceBuilder(string serviceProviderName, IServiceCollection services)
        {
            ServiceProviderName = serviceProviderName;
            _services = services;
            ConfigureDefaultServices();
        }

        public AuthorizationCodeFlowServiceBuilder ConfigureDefaultServices()
        {
            if (string.IsNullOrWhiteSpace(ServiceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(ServiceProviderName));
            }

            _urlProviderBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationUrlProvider, AuthorizationCodeFlowUrlProvider>(ServiceProviderName);
            };
            _tokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationTokenExchanger, AuthorizationCodeFlowExchangeToken>(ServiceProviderName);
            };
            _refreshTokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationRefreshTokenExchanger, AuthorizationCodeFlowExchangeRefreshToken>(ServiceProviderName);
            };
            
            return this;
        }

        public AuthorizationCodeFlowServiceBuilder ConfigureUrlProvider<TService>()
            where TService : class, IOAuthAuthorizationUrlProvider
        {
            _urlProviderBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationUrlProvider, TService>(ServiceProviderName);
            };
            return this;
        }
        public AuthorizationCodeFlowServiceBuilder ConfigureTokenExchanger<TService>()
            where TService : class, IOAuthAuthorizationTokenExchanger
        {
            _tokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationTokenExchanger, TService>(ServiceProviderName);
            };
            return this;
        }

        public AuthorizationCodeFlowServiceBuilder ConfigureRefreshTokenExchanger<TService>()
            where TService : class, IOAuthAuthorizationRefreshTokenExchanger
        {
            _refreshTokenExchangerBuilder = services =>
            {
                services.AddKeyedScoped<IOAuthAuthorizationRefreshTokenExchanger, TService>(ServiceProviderName);
            };
            return this;
        }

        public void Build()
        {
            _urlProviderBuilder?.Invoke(_services);
            _tokenExchangerBuilder?.Invoke(_services);
            _refreshTokenExchangerBuilder?.Invoke(_services);
            
            _services.AddKeyedScoped<IAccessTokenBuilder, AuthorizationCodeFlowAccessTokenBuilder>(ServiceProviderName);
            _services.AddKeyedScoped<IProxyApiMapper, OAuthAuthorizationCodeFlowApiMapper>(ServiceProviderName);

        }
    }
}
