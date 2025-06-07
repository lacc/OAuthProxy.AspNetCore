using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;

namespace OAuthProxy.AspNetCore.Services
{
    public class ThirdPartyServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ThirdPartyServiceConfig> _configs;
        private readonly ILogger<ThirdPartyServiceFactory> _logger;

        public ThirdPartyServiceFactory(IServiceProvider serviceProvider, Dictionary<string, ThirdPartyServiceConfig> configs, ILogger<ThirdPartyServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configs = configs;
            _logger = logger;
        }

        /// <summary>
        /// Gets the IThirdPartyService client for the specified service name.
        /// </summary>
        /// <param name="serviceName">The name of the third-party service (e.g., "ServiceA").</param>
        /// <returns>The IThirdPartyService implementation.</returns>
        /// <exception cref="ArgumentException">Thrown if the service name is not found.</exception>
        public IThirdPartyOAuthService GetService(string serviceName)
        {
            var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetKeyedService<IThirdPartyOAuthService>(serviceName);

            if (service != null)
            {
                return service;
            }

            _logger.LogError("Third-party service '{ServiceName}' not found.", serviceName);
            throw new ArgumentException($"Third-party service '{serviceName}' not found.");
        }

        /// <summary>
        /// Gets the configuration for the specified service name.
        /// </summary>
        /// <param name="serviceName">The name of the third-party service.</param>
        /// <returns>The <see cref="ThirdPartyServiceConfig"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the service name is not found.</exception>
        public ThirdPartyServiceConfig GetServiceConfig(string serviceName)
        {
            if (_configs.TryGetValue(serviceName, out var config))
            {
                return config;
            }

            _logger.LogError("Configuration for third-party service '{ServiceName}' not found.", serviceName);
            throw new ArgumentException($"Configuration for third-party service '{serviceName}' not found.");
        }
    }
}
