using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services.CustomFlow
{
    public class CustomFlowServiceBuilder : IBuilder
    {
        private readonly string _serviceProviderName;
        private readonly IServiceCollection _services;
        private Action<IServiceCollection>? _accessTokenBuilder = null;
        private Action<IServiceCollection>? _customServicesBuilder = null;

        public CustomFlowServiceBuilder(string serviceProviderName, IServiceCollection services)
        {
            _serviceProviderName = serviceProviderName;
            _services = services;

            ConfigureDefaultServices();
        }

        protected virtual void ConfigureDefaultServices()
        {
            if (string.IsNullOrWhiteSpace(_serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(_serviceProviderName));
            }
        }

        public CustomFlowServiceBuilder ConfigureAccessTokenBuilder<TAccesTokenBuilder>()
            where TAccesTokenBuilder : class, IAccessTokenBuilder
        {
            _accessTokenBuilder = services =>
            {
                services.AddKeyedScoped<IAccessTokenBuilder, TAccesTokenBuilder>(_serviceProviderName);
            };
            return this;
        }

        public CustomFlowServiceBuilder ConfigureCustomServices(Action<IServiceCollection> customServicesBuilder)
        {
            _customServicesBuilder = customServicesBuilder ?? throw new ArgumentNullException(nameof(customServicesBuilder));
            return this;
        }

        public void Build()
        {
            if (string.IsNullOrWhiteSpace(_serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(_serviceProviderName));
            }

            if (_accessTokenBuilder == null)
            {
                throw new InvalidOperationException("Access token builder is not configured. Please configure it before building.");
            }
            _accessTokenBuilder.Invoke(_services);

            _customServicesBuilder?.Invoke(_services);
        }
    }
}
