using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.UserServices;

namespace OAuthProxy.AspNetCore.Extensions
{
    public class OAuthProxyServiceBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthProxyServiceBuilder> _logger;
        private Action<DbContextOptionsBuilder>? _optionsAction;
        private const string HttpClientName = "OAuthProxyHttpClient";
        private const int DefaultHttpClientTimeoutSeconds = 30;

        public OAuthProxyServiceBuilder(IServiceCollection services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
            _logger = services.BuildServiceProvider().GetRequiredService<ILogger<OAuthProxyServiceBuilder>>();
        }

        internal OAuthProxyServiceBuilder ConfigureThirdPartyOAuthProxyServices(string thirdPartyClientConfigKey)
        {
            var clientConfigSection = _configuration.GetSection(thirdPartyClientConfigKey);
            if (clientConfigSection == null || !clientConfigSection.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{thirdPartyClientConfigKey}' not found in appsettings.");
            }

            _services.Configure<Dictionary<string, ThirdPartyServiceConfig>>(
                clientConfigSection);
            
            _services.AddHttpClient(HttpClientName)
                .ConfigureHttpClient(client =>
                {
                    var timeout = _configuration.GetValue<int?>("HttpClientTimeoutSeconds") ?? 
                                        DefaultHttpClientTimeoutSeconds;
                    
                    _logger.LogDebug("Setting HTTP client timeout to {Timeout} seconds", timeout);
                    client.Timeout = TimeSpan.FromSeconds(timeout);
                });

            _services.AddScoped<TokenStorageService>();
            _services.AddScoped<OAuthService>();
            _services.AddSingleton(serviceProvider =>
            {
                var configs = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dictionary<string, ThirdPartyServiceConfig>>>().Value;
                var logger = serviceProvider.GetRequiredService<ILogger<ThirdPartyServiceFactory>>();
                return new ThirdPartyServiceFactory(serviceProvider, configs, logger);
            });

            return this;
        }

        /// <summary>
        /// Configures the OAuth proxy to use a custom database configuration.
        /// </summary>
        /// <param name="optionsAction">The action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <param name="autoMigration">Whether to automatically apply migrations on startup.</param>
        /// <returns>The <see cref="OAuthProxyServiceBuilder"/> for chaining.</returns>
        public OAuthProxyServiceBuilder WithTokenStorageDatabase(Action<DbContextOptionsBuilder>? optionsAction, bool autoMigration = true)
        {
            if (optionsAction != null)
            {
                _services.AddDbContext<TokenDbContext>(optionsAction);
                if (autoMigration)
                {
                    _logger.LogInformation("Configuring automatic database migrations for OAuth token storage.");
                    _services.AddHostedService<DatabaseMigrationService>();
                }
            }
            else
            {
                _logger.LogWarning("No database configuration provided. Using in-memory database for token storage. Only for testing purposes!");
                _services.AddDbContext<TokenDbContext>(options => options.UseInMemoryDatabase("OAuthProxyTokens"));
            }

            return this;
        }

        /// <summary>
        /// Configures the <see cref="OAuthProxyServiceBuilder"/> to use the specified implementation of <see
        /// cref="IUserIdProvider"/>.
        /// </summary>
        /// <remarks>This method registers the specified <typeparamref name="TUserIdProvider"/> as a
        /// singleton service  for the <see cref="IUserIdProvider"/> interface in the dependency injection
        /// container.</remarks>
        /// <typeparam name="TUserIdProvider">The type of the user ID provider to register. Must implement <see cref="IUserIdProvider"/> and be a class.</typeparam>
        /// <returns>The current instance of <see cref="OAuthProxyServiceBuilder"/> to allow method chaining.</returns>
        public OAuthProxyServiceBuilder WithUserIdProvider<TUserIdProvider>()
            where TUserIdProvider : class, IUserIdProvider
        {
            _services.AddSingleton<IUserIdProvider, TUserIdProvider>();
            return this;
        }

        /// <summary>
        /// Configures the <see cref="OAuthProxyServiceBuilder"/> to use the default JWT-based implementation of <see
        /// cref="IUserIdProvider"/>.
        /// </summary>
        /// <remarks>This method registers the <see cref="JwtUserIdProvider"/> as the scoped
        /// implementation of  <see cref="IUserIdProvider"/> in the dependency injection container. Use this method if 
        /// user identification is based on JWT tokens.</remarks>
        /// <returns>The current instance of <see cref="OAuthProxyServiceBuilder"/> to allow method chaining.</returns>
        public OAuthProxyServiceBuilder WithDefaultJwtUserIdProvider()
        {
            _services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            _services.AddScoped<IUserIdProvider, JwtUserIdProvider>();
            return this;
        }

        /// <summary>
        /// Adds a third-party OAuth service client to the proxy.
        /// </summary>
        /// <typeparam name="TServiceClient">The type of the service client.</typeparam>
        /// <param name="serviceProviderName">The name of the service provider. Must align with the key in the appsettings config</param>
        /// <returns>The <see cref="OAuthProxyServiceBuilder"/> for chaining.</returns>
        public OAuthProxyServiceBuilder WithOAuthServiceClient<TServiceClient>(string serviceProviderName)
            where TServiceClient : class, IThirdPartyOAuthService
        {
            if(string.IsNullOrWhiteSpace(serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(serviceProviderName));
            }

            _services.AddKeyedScoped<IThirdPartyOAuthService, TServiceClient>(serviceProviderName, (serviceProvider, o) =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(HttpClientName);

                var configs = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dictionary<string, ThirdPartyServiceConfig>>>().Value;
                if (configs == null || configs.Count == 0)
                {
                    throw new InvalidOperationException("Third-party service configurations are not available.");
                }

                var serviceConfig = configs.TryGetValue(serviceProviderName, out var config) ? config : null;
                if (serviceConfig == null)
                {
                    throw new ArgumentException($"Configuration for service '{serviceProviderName}' not found.");
                }

                var logger = serviceProvider.GetRequiredService<ILogger<TServiceClient>>();
                return (TServiceClient)Activator.CreateInstance(typeof(TServiceClient), serviceProviderName, httpClient, serviceConfig, logger)!;
            });

            return this;
        }
    }
}
