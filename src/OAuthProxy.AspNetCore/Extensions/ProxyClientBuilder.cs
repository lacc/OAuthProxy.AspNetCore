using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow;

namespace OAuthProxy.AspNetCore.Extensions
{
    public class ProxyClientBuilder<TClient> : IProxyClientBuilder, IRegisteredProxyProviders
        where TClient : class
    {
        public const int DefaultHttpClientTimeoutSeconds = 30;

        private readonly ProxyClientBuilderOption<TClient> _builderOption;
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;
        private readonly string _configPrefix;
        private readonly Dictionary<Type, Func<IServiceProvider, DelegatingHandler>?> _messageHandlerFactories = [];

        public string ServiceProviderName { get; }
        public bool AllowHttpRedirects { get; set; } = false;
        private string DefaultConfigKey { get; }

        public ProxyClientBuilder(string serviceProviderName, IServiceCollection services, IConfiguration configuration, string configPrefix)
        {
            ServiceProviderName = serviceProviderName;
            _services = services;
            _configuration = configuration;
            _configPrefix = configPrefix;
            DefaultConfigKey = $"{_configPrefix}:{ServiceProviderName}";

            if (string.IsNullOrWhiteSpace(serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(serviceProviderName));
            }

            _builderOption = new ProxyClientBuilderOption<TClient>
            {
                ServiceProviderName = serviceProviderName
            };
        }

        public ProxyClientBuilder<TClient> WithAuthorizationConfig(IConfigurationSection configurationSection)
        {
            _builderOption.OAuthConfiguration = new ThirdPartyServiceConfig();
            configurationSection.Bind(_builderOption.OAuthConfiguration);

            return this;
        }

        public ProxyClientBuilder<TClient> WithAuthorizationCodeFlow(IConfigurationSection? configurationSection = null, Action<AuthorizationCodeFlowServiceBuilder>? authorizationFlowBuilder = null)
        {
            if (configurationSection != null)
            {
                WithAuthorizationConfig(configurationSection);
            }

            var flowBuilder = new AuthorizationCodeFlowServiceBuilder(_builderOption.ServiceProviderName, _services);
            authorizationFlowBuilder?.Invoke(flowBuilder);
            _builderOption.AuthorizationFlowBuilder = flowBuilder;

            return this;
        }

        public ProxyClientBuilder<TClient> AddHttpMessageHandler<TMessageHandler>(Func<IServiceProvider, DelegatingHandler>? handlerFactory = null)
            where TMessageHandler : DelegatingHandler
        {
            if (_messageHandlerFactories.ContainsKey(typeof(TMessageHandler)))
            {
                throw new InvalidOperationException($"A message handler of type {typeof(TMessageHandler).Name} is already registered.");
            }
            _messageHandlerFactories.Add(typeof(TMessageHandler), handlerFactory);
            return this;
        }

        public void Build()
        {
            if (_builderOption.OAuthConfiguration == null)
            {
                _builderOption.OAuthConfiguration = new ThirdPartyServiceConfig();

                var oauthConfig = _configuration.GetSection(DefaultConfigKey).Get<ThirdPartyServiceConfig>();

                _builderOption.OAuthConfiguration = oauthConfig ??
                    throw new InvalidOperationException($"Default Configuration for '{DefaultConfigKey}' not found.");
            }

            if (_builderOption.AuthorizationFlowBuilder == null)
            {
                throw new InvalidOperationException("AuthorizationFlowBuilder must be set before building the client.");
            }
            _builderOption.AuthorizationFlowBuilder?.Build();

            _services.Configure<ThirdPartyProviderConfig>(_builderOption.ServiceProviderName, options =>
            {
                options.ServiceProviderName = _builderOption.ServiceProviderName;
                options.OAuthConfiguration = _builderOption.OAuthConfiguration;
                options.AllowHttpRedirects = AllowHttpRedirects;
            });

            _services.AddScoped<TClient>();

            foreach (var handlerFactoryConfig in _messageHandlerFactories)
            {
                var handlerObjectType = handlerFactoryConfig.Key;
                if (handlerFactoryConfig.Value == null)
                {
                    _services.AddScoped(handlerObjectType);
                    _services.AddKeyedScoped(handlerObjectType, _builderOption.ServiceProviderName);
                }
                else
                {
                    _services.AddKeyedScoped(handlerObjectType, _builderOption.ServiceProviderName, 
                        (sp, o) =>
                        {
                            var handlerFactory = handlerFactoryConfig.Value(sp);
                            if (handlerFactory is null)
                            {
                                throw new InvalidOperationException($"The handler factory for type {handlerObjectType.Name} returned null. " +
                                                                    "Ensure the factory function returns a valid handler instance.");
                            }
                            return handlerFactory;
                        });
                }
            }

            var httpClientBuilder = _services
                .AddHttpClient(_builderOption.ServiceProviderName, client =>
                {
                    var timeout = _configuration.GetValue<int?>("HttpClientTimeoutSeconds") ??
                                        DefaultHttpClientTimeoutSeconds;

                    client.Timeout = TimeSpan.FromSeconds(timeout);
                    client.BaseAddress = new Uri(_builderOption.OAuthConfiguration.ApiBaseUrl);
                })
                .AddAsKeyed()
                .AddHttpMessageHandler((sp) =>
                {
                    sp.GetRequiredService<IProxyRequestContext>()
                        .SetServiceName(_builderOption.ServiceProviderName);

                    var res = sp.GetRequiredService<BasicOAuthBearerTokenHandler>();
                    return res;
                });
            foreach (var handlerFactoryConfig in _messageHandlerFactories)
            {
                httpClientBuilder.AddHttpMessageHandler((sp) =>
                {
                    var handler = sp.GetRequiredKeyedService(handlerFactoryConfig.Key, _builderOption.ServiceProviderName);
                    return (DelegatingHandler)handler;
                });
            }
        }
    }
}
