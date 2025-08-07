using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services
{
    public class AuthorizationFlowServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AuthorizationFlowServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOAuthAuthorizationUrlProvider GetAuthorizationUrlProvider(string serviceName)
        {
            var res = _serviceProvider.GetKeyedService<IOAuthAuthorizationUrlProvider>(serviceName);
            return res ?? throw new InvalidOperationException($"No authorization URL provider registered for service '{serviceName}'.");
        }

        public IOAuthAuthorizationTokenExchanger GetAuthorizationTokenExchanger(string serviceName)
        {
            var res = _serviceProvider.GetKeyedService<IOAuthAuthorizationTokenExchanger>(serviceName);
            return res ?? throw new InvalidOperationException($"No authorization token exchanger registered for service '{serviceName}'.");
        }
        public IClientCredentialsTokenExchanger GetClientCredentialsTokenExchanger(string serviceName)
        {
            var res = _serviceProvider.GetKeyedService<IClientCredentialsTokenExchanger>(serviceName);
            return res ?? throw new InvalidOperationException($"No authorization token exchanger registered for service '{serviceName}'.");
        }
        public IOAuthAuthorizationRefreshTokenExchanger GetAuthorizationRefreshTokenExchanger(string serviceName)
        {
            var res = _serviceProvider.GetKeyedService<IOAuthAuthorizationRefreshTokenExchanger>(serviceName);
            return res ?? throw new InvalidOperationException($"No authorization refresh token exchanger registered for service '{serviceName}'.");
        }
    }
}
