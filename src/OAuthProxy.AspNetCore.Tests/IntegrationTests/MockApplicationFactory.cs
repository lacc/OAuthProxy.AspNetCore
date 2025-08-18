using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Extensions;

namespace OAuthProxy.AspNetCore.Tests.IntegrationTests
{
    class TestProvider { }
    internal static class MockApplicationFactory
    {
        public static WebApplicationFactory<MockApplication> GetFactory(this WebApplicationFactory<MockApplication> factory, string name, bool withAuthenicatedUser = true, bool disableStateValidation = false)
        {
            var res = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    // Add in-memory configuration for the required section
                    var testConfig = new Dictionary<string, string?>
                    {
                        [$"ThirdPartyServices:{name}:AuthorizeEndpoint"] = "https://auth.example.com/authorize",
                        [$"ThirdPartyServices:{name}:TokenEndpoint"] = "https://auth.example.com/token",
                        [$"ThirdPartyServices:{name}:ClientId"] = "test-client-id",
                        [$"ThirdPartyServices:{name}:ClientSecret"] = "test-client-secret",
                        [$"ThirdPartyServices:{name}:Scope"] = "openid profile email",
                        [$"ThirdPartyServices:{name}:Name"] = name
                    };
                    configBuilder.AddInMemoryCollection(testConfig);
                });

                builder.ConfigureServices((context, services) =>
                {
                    if (withAuthenicatedUser)
                    {
                        services.AddAuthentication("Test")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                    }
                    else
                    {
                        services.AddAuthentication("Test")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerNoResult>("Test", _ => { });
                    }

                    services.AddThirdPartyOAuthProxy(context.Configuration, proxyBuilder =>
                            proxyBuilder
                                .WithTokenStorageOptions(options =>
                                {
                                    options.AutoMigration = false;
                                    options.DatabaseOptions = dbOptions =>
                                        dbOptions.UseInMemoryDatabase("TestDb_" + "Test_" + name);
                                })
                                .AddOAuthServiceClient<TestProvider>(name, clientBuilder =>
                                {
                                    clientBuilder.AllowHttpRedirects = true;
                                    clientBuilder.WithAuthorizationCodeFlow(
                                        context.Configuration.GetSection($"ThirdPartyServices:{name}"),
                                        b =>
                                        {
                                            b.ConfigureTokenExchanger<DummyCodeExchanger>();
                                            if (disableStateValidation)
                                            {
                                                b.DisableStateValidation();
                                            }
                                        });
                                })
                        );

                    services.AddAuthorization();
                });


            });

            return res;
        }

    }
}
