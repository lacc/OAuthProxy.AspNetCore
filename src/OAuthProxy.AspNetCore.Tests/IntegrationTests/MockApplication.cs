using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Extensions;
using OAuthProxy.AspNetCore.Tests.IntegrationTests;

// Minimal Program class for integration testing the SDK package
var builder = WebApplication.CreateBuilder(args);

// Add dummy authentication and authorization for testing
builder.Services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();

// Register the SDK with test-friendly options (can be overridden in tests)
builder.Services.AddThirdPartyOAuthProxy(builder.Configuration, proxyBuilder =>
{
    proxyBuilder
        .WithTokenStorageOptions(options =>
        {
            options.AutoMigration = false;
            // The test can override this to use InMemory
        });
    // No clients registered here; tests can add them via ConfigureServices
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Map the proxy endpoints (this will map all registered OAuth endpoints)
app.MapProxyClientEndpoints();

app.Run();

// For WebApplicationFactory<T>
public partial class MockApplication { }
