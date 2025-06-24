using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Extensions;
using System.Net;

namespace OAuthProxy.AspNetCore.Tests.IntegrationTests
{
    // Use the Program class from THIS test project, not from the Demo project
    public class OAuthAuthorizationCodeFlowApiMapperIntegrationTest : IClassFixture<WebApplicationFactory<MockApplication>>
    {
        private readonly WebApplicationFactory<MockApplication> _factory;
        class TestProvider {}
        public OAuthAuthorizationCodeFlowApiMapperIntegrationTest(WebApplicationFactory<MockApplication> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    // Add in-memory configuration for the required section
                    var testConfig = new Dictionary<string, string?>
                    {
                        ["ThirdPartyServices:TestProvider:AuthorizeEndpoint"] = "https://auth.example.com/authorize",
                        ["ThirdPartyServices:TestProvider:TokenEndpoint"] = "https://auth.example.com/token",
                        ["ThirdPartyServices:TestProvider:ClientId"] = "test-client-id",
                        ["ThirdPartyServices:TestProvider:ClientSecret"] = "test-client-secret",
                        ["ThirdPartyServices:TestProvider:Scope"] = "openid profile email"
                    };
                    configBuilder.AddInMemoryCollection(testConfig);
                });

                builder.ConfigureServices((context, services) =>
                {
                    // Re-register the proxy with in-memory DB and dummy exchanger for test
                    services.AddThirdPartyOAuthProxy(context.Configuration, proxyBuilder =>
                        proxyBuilder
                            .WithTokenStorageOptions(options =>
                            {
                                options.AutoMigration = false;
                                options.DatabaseOptions = dbOptions =>
                                    dbOptions.UseInMemoryDatabase("TestDb_" + "Test");
                            })
                            .AddOAuthServiceClient<TestProvider>("TestProvider", clientBuilder =>
                                clientBuilder.WithAuthorizationCodeFlow(
                                    context.Configuration.GetSection("ThirdPartyServices:TestProvider"),
                                    b => b.ConfigureTokenExchanger<DummyCodeExchanger>()
                                )
                            )
                    );
                });
            });
        }

        [Fact]
        public async Task Authorize_WithLocalRedirectUri_And_Callback_FullFlow()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var localRedirectUrl = "http://localhost/after";
            var encodedRedirectUrl = System.Web.HttpUtility.UrlEncode(localRedirectUrl);
            // Act: Call /oauth/{provider}/authorize?local_redirect_uri=...
            var authorizeResp = await client.GetAsync($"/api/proxy/testprovider/authorize?local_redirect_uri={encodedRedirectUrl}");
            Assert.Equal(HttpStatusCode.TemporaryRedirect, authorizeResp.StatusCode);

            var location = authorizeResp.Headers.Location.ToString();
            Assert.Contains("state=", location);

            // Extract state from redirect URL
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["state"];
            Assert.False(string.IsNullOrEmpty(state));

            // Simulate callback
            var callbackResp = await client.GetAsync($"/api/proxy/testprovider/callback?code=thecode&state={state}");
            Assert.Equal(HttpStatusCode.PermanentRedirect, callbackResp.StatusCode);

            // Check that the redirect is to the local_redirect_uri
            var callbackLocation = callbackResp.Headers.Location.ToString();
            Assert.Equal("http://localhost/after", callbackLocation);

            // Check that token is saved in the db
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TokenDbContext>();
            var token = await db.OAuthTokens.FirstOrDefaultAsync();
            Assert.NotNull(token);
            Assert.Equal("access", token.AccessToken);
            Assert.False(db.LocalRedirectUris.Any());
        }

        [Fact]
        public async Task Authorize_WithoutLocalRedirectUri_And_Callback_FullFlow()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act: Call /oauth/{provider}/authorize without local_redirect_uri
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            var authorizeResp = await client.GetAsync("/api/proxy/TestProvider/authorize");
            Assert.Equal(HttpStatusCode.OK, authorizeResp.StatusCode);

            var location = await authorizeResp.Content.ReadAsStringAsync();
            location = location.Trim('"'); // Remove quotes if any
            Assert.Contains("state=", location);

            // Extract state from redirect URL
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["state"];
            Assert.False(string.IsNullOrEmpty(state));

            // Simulate callback
            var callbackResp = await client.GetAsync($"/api/proxy/TestProvider/callback?code=thecode&state={state}");
            Assert.Equal(HttpStatusCode.OK, callbackResp.StatusCode);

            // Check that token is saved in the db
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TokenDbContext>();
            var token = await db.OAuthTokens.FirstOrDefaultAsync();
            Assert.NotNull(token);
            Assert.Equal("access", token.AccessToken);

            Assert.False(db.LocalRedirectUris.Any());
        }
    }
}