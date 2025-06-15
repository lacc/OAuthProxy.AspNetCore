using OAuthProxy.AspNetCore.Abstractions;

namespace OAuthProxy.AspNetCore.Services
{
    internal class ProxyRequestContext : IProxyRequestContext
    {
        private string? _serviceName = null;

        public ProxyRequestContext()
        {
        }

        public void SetServiceName(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
            }
            _serviceName = serviceName;
        }

        public string GetServiceName()
        {
            if (string.IsNullOrEmpty(_serviceName))
            {
                throw new InvalidOperationException("Service name is not set.");
            }
            return _serviceName;
        }
    }
}
