using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Extensions;
using System.Net;

namespace OAuthProxy.AspNetCore.Tests.IntegrationTests
{
    public class OAuthAuthorizationCodeFlowApiMapperIntegrationTest_DisableState : IClassFixture<WebApplicationFactory<MockApplication>>
    {
        private readonly WebApplicationFactory<MockApplication> _factory;
        
        public OAuthAuthorizationCodeFlowApiMapperIntegrationTest_DisableState(WebApplicationFactory<MockApplication> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private WebApplicationFactory<MockApplication> GetFactory(string name, bool withAuthenicatedUser)
        {
            return _factory.GetFactory(name, withAuthenicatedUser, true);
        }

        [Fact]
        public async Task Authorize_And_Callback_FullFlow_DisabledState_Succeed()
        {
            var webAppFactory = GetFactory("TestProvider", true);

            using var scope = webAppFactory.Services.CreateScope();
            var client = webAppFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            // Act: Call /oauth/{provider}/authorize...
            // We don't send the redirect uri as it does not take effect if state validation is disabled
            var authorizeResp = await client.GetAsync($"/api/proxy/testprovider/authorize");
            Assert.Equal(HttpStatusCode.TemporaryRedirect, authorizeResp.StatusCode);

            var location = authorizeResp?.Headers?.Location?.ToString();
            Assert.NotNull(location);
            Assert.Contains("state=", location);

            // Extract state from redirect URL
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["state"];
            Assert.False(string.IsNullOrEmpty(state));

            // Simulate callback
            var callbackResp = await client.GetAsync($"/api/proxy/testprovider/callback?code=thecode&state={state}");
            Assert.Equal(HttpStatusCode.OK, callbackResp.StatusCode); //no redirection

            // Check that token is saved in the db
            var db = scope.ServiceProvider.GetRequiredService<TokenDbContext>();
            var token = await db.OAuthTokens.FirstOrDefaultAsync();
            Assert.NotNull(token);
            Assert.Equal("access", token.AccessToken);
        }

        [Fact]
        public async Task Authorize_And_Callback_FullFlow_DisabledState_NoUserId_Returns_UnAuthorized()
        {
            var webAppFactory = GetFactory("TestProvider", false);

            using var scope = webAppFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TokenDbContext>();
            var tokenCount = await db.OAuthTokens.CountAsync();

            var client = webAppFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            // Act: Call /oauth/{provider}/authorize...
            var localRedirectUrl = "http://localhost/after";
            var encodedRedirectUrl = System.Web.HttpUtility.UrlEncode(localRedirectUrl);
            // Act: Call /oauth/{provider}/authorize?local_redirect_uri=...
            var authorizeResp = await client.GetAsync($"/api/proxy/testprovider/authorize?local_redirect_uri={encodedRedirectUrl}");
            Assert.Equal(HttpStatusCode.TemporaryRedirect, authorizeResp.StatusCode);

            var location = authorizeResp?.Headers?.Location?.ToString();
            Assert.NotNull(location);
            Assert.Contains("state=", location);

            // Extract state from redirect URL
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["state"];
            Assert.False(string.IsNullOrEmpty(state));

            // Simulate callback
            var callbackResp = await client.GetAsync($"/api/proxy/testprovider/callback?code=thecode&state={state}");
            Assert.Equal(HttpStatusCode.Unauthorized, callbackResp.StatusCode); //no redirection

            // Check that token is saved in the db
            Assert.Equal(tokenCount, await db.OAuthTokens.CountAsync());
        }
    }
}