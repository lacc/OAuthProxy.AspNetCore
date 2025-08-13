using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Extensions;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.UserServices;

namespace OAuthProxy.AspNetCore.Tests
{
    public class ThirdPartyOAuthProxyBuilderTest
    {
        private class DummyClient
        {
            public DummyClient([FromKeyedServices("ProviderA")] HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; }
        }
        private class CustomUserIdProvider : IUserIdProvider
        {
            public string GetCurrentUserId() => "custom";
        }

        [Fact]
        public void WithTokenStorageOptions_ConfiguresDbContextAndService()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            bool dbOptionsCalled = false;
            builder.WithTokenStorageOptions(opt =>
            {
                dbOptionsCalled = true;
                opt.AutoMigration = false;
            });

            services.AddScoped<IRefreshTokenService>(sp => new Mock<IRefreshTokenService>().Object);

            var provider = services.BuildServiceProvider();
            Assert.True(dbOptionsCalled);
            Assert.NotNull(provider.GetService<ITokenStorageService>());
            Assert.NotNull(provider.GetService<TokenDbContext>());
        }

        [Fact]
        public void WithTokenStorageOptions_AddsHostedService_WhenAutoMigrationTrue()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.WithTokenStorageOptions(opt =>
            {
                opt.AutoMigration = true;
            });

            var provider = services.BuildServiceProvider();
            // HostedService is registered as IHostedService, but we check for DatabaseMigrationService type
            var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            Assert.Contains(hostedServices, s => s.GetType().Name.Contains("DatabaseMigrationService"));
        }

        [Fact]
        public void WithDefaultJwtUserIdProvider_RegistersJwtUserIdProvider()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.WithDefaultJwtUserIdProvider();
            builder.Build();

			var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IUserIdProvider>());
            Assert.IsType<JwtUserIdProvider>(provider.GetService<IUserIdProvider>());
        }

        [Fact]
        public void WithUserIdProvider_RegistersCustomProvider()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.WithUserIdProvider<CustomUserIdProvider>();
            builder.Build();

			var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IUserIdProvider>());
            Assert.IsType<CustomUserIdProvider>(provider.GetService<IUserIdProvider>());
        }

		[Fact]
		public void WithDefaultJwtUserIdProvider_SecondCallOverridesDefault()
		{
			var services = new ServiceCollection();
			var config = new ConfigurationBuilder().Build();
			var builder = new ThirdPartyOAuthProxyBuilder(services, config);

			builder.WithDefaultJwtUserIdProvider();
			builder.WithUserIdProvider<CustomUserIdProvider>();
            builder.Build();

			var provider = services.BuildServiceProvider();
			var userIdProvider = provider.GetService<IUserIdProvider>();
			Assert.NotNull(userIdProvider);
			Assert.IsType<CustomUserIdProvider>(userIdProvider);
		}

        [Fact]
        public void AddOAuthServiceClient_ThrowsIfNameNullOrEmpty()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            Assert.Throws<ArgumentException>(() => builder.AddOAuthServiceClient<DummyClient>("", null));
        }

        [Fact]
        public void AddOAuthServiceClient_ThrowsIfAlreadyConfigured()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.AddOAuthServiceClient<DummyClient>("ProviderA");
            Assert.Throws<InvalidOperationException>(() => builder.AddOAuthServiceClient<DummyClient>("ProviderA"));
        }

        [Fact]
        public void Build_RegistersCoreServices_AndBuildsAuthorizationCodeFlowClients()
        {
            var services = new ServiceCollection();
			

			var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "OAuth:ProviderA:ClientId", "id" },
                    { "OAuth:ProviderA:ClientSecret", "secret" },
                    { "OAuth:ProviderA:TokenEndpoint", "https://token" },
                    { "OAuth:ProviderA:AuthorizeEndpoint", "https://auth" },
                    { "OAuth:ProviderA:RedirectUri", "https://redirect" },
                    { "OAuth:ProviderA:ApiBaseUrl", "https://api" },
                    { "OAuth:ProviderA:Scopes", "scope" } 
                }).Build();

			var builder = new ThirdPartyOAuthProxyBuilder(services, config);

			builder.WithTokenStorageOptions(opt =>
			{
				opt.AutoMigration = false;
			});

			var clientBuilder = new ThirdPartyOAuthProxyBuilder(services, config);

			builder.AddOAuthServiceClient<DummyClient>("ProviderA", clientBuilder =>
            {
                clientBuilder.WithAuthorizationCodeFlow(config.GetSection("OAuth:ProviderA"));
			});

            builder.Build();

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<DummyClient>());
            var httpClient = provider.GetService<DummyClient>()?.HttpClient;
			Assert.NotNull(httpClient);
		}
        [Fact]
        public void Build_RegistersCoreServices_AndBuildsClientCredentialsClients()
        {
            var services = new ServiceCollection();


            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "OAuth:ProviderA:ClientId", "id" },
                    { "OAuth:ProviderA:ClientSecret", "secret" },
                    { "OAuth:ProviderA:TokenEndpoint", "https://token" },
                    { "OAuth:ProviderA:ApiBaseUrl", "https://api" },
                    { "OAuth:ProviderA:Scopes", "scope" }
                }).Build();

            var builder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.WithTokenStorageOptions(opt =>
            {
                opt.AutoMigration = false;
            });

            var clientBuilder = new ThirdPartyOAuthProxyBuilder(services, config);

            builder.AddOAuthServiceClient<DummyClient>("ProviderA", clientBuilder =>
            {
                clientBuilder.WithClientCredentialsFlow(config.GetSection("OAuth:ProviderA"));
            });

            builder.Build();

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<DummyClient>());
            var httpClient = provider.GetService<DummyClient>()?.HttpClient;
            Assert.NotNull(httpClient);
        }

        [Fact]
        public void Build_UsesDefaultJwtUserIdProvider_IfNotConfigured()
        {
            var services = new ServiceCollection();
			var config = new ConfigurationBuilder().AddInMemoryCollection(
				new Dictionary<string, string?>
				{
					{ "OAuth:ProviderA:ClientId", "id" },
					{ "OAuth:ProviderA:ClientSecret", "secret" },
					{ "OAuth:ProviderA:TokenEndpoint", "https://token" },
					{ "OAuth:ProviderA:AuthorizeEndpoint", "https://auth" },
					{ "OAuth:ProviderA:RedirectUri", "https://redirect" },
					{ "OAuth:ProviderA:ApiBaseUrl", "https://api" },
					{ "OAuth:ProviderA:Scopes", "scope" }
				}).Build();
			var builder = new ThirdPartyOAuthProxyBuilder(services, config);

			builder.AddOAuthServiceClient<DummyClient>("ProviderA", clientBuilder =>
			{
				clientBuilder.WithAuthorizationCodeFlow(config.GetSection("OAuth:ProviderA"));
			});

			builder.Build();

            var provider = services.BuildServiceProvider();
            var userIdProvider = provider.GetService<IUserIdProvider>();
            Assert.NotNull(userIdProvider);
            Assert.IsType<JwtUserIdProvider>(userIdProvider);
        }
    }
}
