using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.StateManagement;
using OAuthProxy.AspNetCore.Services.UserServices;

namespace OAuthProxy.AspNetCore.Extensions
{
    public class ThirdPartyOAuthProxyBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        OAuthProxyConfiguration BuilderOptions { get; }
        public ThirdPartyOAuthProxyBuilder(IServiceCollection services, IConfiguration configuration)
        {
            BuilderOptions = new OAuthProxyConfiguration();
            _services = services;
            _configuration = configuration;
        }

        public ThirdPartyOAuthProxyBuilder WithTokenStorageOptions(Action<TokenStorageConfiguration> dbOptions)
        {
            dbOptions?.Invoke(BuilderOptions.TokenStorageConfiguration);
            _services.AddDbContext<TokenDbContext>(BuilderOptions.TokenStorageConfiguration.DatabaseOptions);
            if (BuilderOptions.TokenStorageConfiguration.AutoMigration)
            {
                //_logger.LogInformation("Configuring automatic database migrations for OAuth token storage.");
                _services.AddHostedService<DatabaseMigrationService>();
            }
            _services.AddScoped<ITokenStorageService, TokenStorageService>();
            return this;
        }
        public ThirdPartyOAuthProxyBuilder WithDefaultJwtUserIdProvider()
        {
            BuilderOptions.UserIdProvider = (services) =>
            {
                services.AddHttpContextAccessor();
                services.AddScoped<IUserIdProvider, JwtUserIdProvider>();
            };

            return this;
        }
        public ThirdPartyOAuthProxyBuilder WithUserIdProvider<TUserIdProvider>()
            where TUserIdProvider : class, IUserIdProvider
        {
            BuilderOptions.UserIdProvider = (services) =>
            {
                services.AddHttpContextAccessor();
                services.AddScoped<IUserIdProvider, TUserIdProvider>();
            };

            return this;
        }
        public ThirdPartyOAuthProxyBuilder ConfigureDataProtector(Action<IDataProtectionBuilder> builder)
        {
            var dataProtectorBuilder = _services.AddDataProtection();
            builder.Invoke(dataProtectorBuilder);

            return this;
        }
        public ThirdPartyOAuthProxyBuilder ConfigureDrefaultDataProtector()
        {
            _services.AddDataProtection()
                .SetApplicationName("OAuthProxyDemo")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
                .SetDefaultKeyLifetime(TimeSpan.FromDays(60));
            return this;
        }
        public ThirdPartyOAuthProxyBuilder AddOAuthServiceClient<TServiceClient>(string serviceProviderName, Action<ProxyClientBuilder<TServiceClient>>? clientBuilderAction = null)
            where TServiceClient : class
        {
            if (string.IsNullOrWhiteSpace(serviceProviderName))
            {
                throw new ArgumentException("Service provider name cannot be null or empty.", nameof(serviceProviderName));
            }
            var serviceConfig = BuilderOptions.ProxyClientBuilders.FirstOrDefault(s => s.ServiceProviderName.Equals(serviceProviderName, StringComparison.OrdinalIgnoreCase));
            if (serviceConfig != null)
            {
                throw new InvalidOperationException($"Service provider '{serviceProviderName}' is already configured.");
            }

            var clientBuilder = new ProxyClientBuilder<TServiceClient>(serviceProviderName, _services, _configuration, BuilderOptions.ThirdPartyClientConfigKey);
            clientBuilderAction?.Invoke(clientBuilder);

            BuilderOptions.ProxyClientBuilders.Add(clientBuilder);
            _services.AddSingleton<IRegisteredProxyProviders>(clientBuilder);

            return this;
        }

        public void Build()
        {
            _services.AddScoped<IProxyRequestContext, ProxyRequestContext>();
            _services.AddScoped<AuthorizationFlowServiceFactory>();
            _services.AddScoped<IAuthorizationStateService, AuthorizationStateServiceDP>();
            _services.AddScoped< BasicOAuthBearerTokenHandler >();

            if (BuilderOptions.UserIdProvider == null)
            {
                WithDefaultJwtUserIdProvider();
            }

            BuilderOptions.UserIdProvider?.Invoke(_services);

            foreach (var clientBuilder in BuilderOptions.ProxyClientBuilders)
            {
                clientBuilder.Build();

            }


        }
    }
}
