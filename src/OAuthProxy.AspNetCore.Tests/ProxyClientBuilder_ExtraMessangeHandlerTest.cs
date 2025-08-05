using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Extensions;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Services;
using System.Net;

namespace OAuthProxy.AspNetCore.Tests
{
    public class ProxyClientBuilder_ExtraMessangeHandlerTest
    {
        private class  FakeProxyClient
        {
            
        }
        private class ExtraAHeaderHandler : DelegatingHandler
        {
            
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("X-Proxy", "A");
                return base.SendAsync(request, cancellationToken);
            }
        }
        private class ExtraBHeaderHandler : DelegatingHandler
        {

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("X-Proxy", "B");
                return base.SendAsync(request, cancellationToken);
            }
        }
        private class CaptureRequestHandler : DelegatingHandler
        {
            public HttpRequestMessage? CapturedRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        [Fact]
        public async Task AddHttpMessageHandler_ShouldAddExtraHeader_PerProxy()
        {
            // Arrange
            var services = new ServiceCollection();
            var configDict = new Dictionary<string, string>
            {
                ["ThirdPartyClients:ProxyA:ApiBaseUrl"] = "https://example.com/",
                ["ThirdPartyClients:ProxyA:ClientId"] = "idA",
                ["ThirdPartyClients:ProxyA:ClientSecret"] = "secretA",
                ["ThirdPartyClients:ProxyB:ApiBaseUrl"] = "https://example.com/",
                ["ThirdPartyClients:ProxyB:ClientId"] = "idB",
                ["ThirdPartyClients:ProxyB:ClientSecret"] = "secretB"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();
            
            services.AddScoped<IProxyRequestContext, ProxyRequestContext>();
            services.AddScoped<BasicOAuthBearerTokenHandler>();

            var expectedAccessToken = "dummy-access-token";
            var tokenStorageMock = new Mock<ITokenStorageService>();
            tokenStorageMock.Setup(ts => ts.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Models.UserTokenDTO
                {
                    UserId = "test-user",
                    ServiceName = "ProxyA",
                    AccessToken = expectedAccessToken,
                    RefreshToken = "dummy-refresh",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                });
            services.AddSingleton(tokenStorageMock.Object);

            var userIdProvideMock = new Mock<IUserIdProvider>();
            userIdProvideMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
            services.AddSingleton(userIdProvideMock.Object);

            // Handler for ProxyA
            var captureHandlerA = new CaptureRequestHandler();
            services.AddKeyedScoped(typeof(CaptureRequestHandler), "ProxyA", (_, o) => captureHandlerA);
            
            // Handler for ProxyB
            var captureHandlerB = new CaptureRequestHandler();
            services.AddKeyedScoped(typeof(CaptureRequestHandler), "ProxyB", (_, o) => captureHandlerB);
           
            // Register ProxyA
            new ProxyClientBuilder<FakeProxyClient>("ProxyA", services, configuration, "ThirdPartyClients")
                .WithAuthorizationCodeFlow(configuration.GetSection("ThirdPartyClients:ProxyA"))
                .AddHttpMessageHandler<ExtraAHeaderHandler>()
                .Build();

            // Register ProxyB
            new ProxyClientBuilder<FakeProxyClient>("ProxyB", services, configuration, "ThirdPartyClients")
                .WithAuthorizationCodeFlow(configuration.GetSection("ThirdPartyClients:ProxyB"))
                .AddHttpMessageHandler<ExtraBHeaderHandler>()
                .Build();

            // Add HttpClient with capture handler at the end of the pipeline
            services.AddHttpClient("ProxyA")
                .AddHttpMessageHandler(_ => captureHandlerA);
            services.AddHttpClient("ProxyB")
                .AddHttpMessageHandler(_ => captureHandlerB);

            var provider = services.BuildServiceProvider();

            var clientA = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxyA");
            var clientB = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxyB");

            // Act
            await clientA.GetAsync("https://example.com/testA");
            await clientB.GetAsync("https://example.com/testB");

            // Assert
            Assert.NotNull(captureHandlerA.CapturedRequest);
            Assert.True(captureHandlerA.CapturedRequest.Headers.Contains("X-Proxy"));
            Assert.Equal("A", captureHandlerA.CapturedRequest.Headers.GetValues("X-Proxy").Single());
            Assert.Equal(expectedAccessToken, captureHandlerA.CapturedRequest.Headers.Authorization?.Parameter);

            Assert.NotNull(captureHandlerB.CapturedRequest);
            Assert.True(captureHandlerB.CapturedRequest.Headers.Contains("X-Proxy"));
            Assert.Equal("B", captureHandlerB.CapturedRequest.Headers.GetValues("X-Proxy").Single());
            Assert.Equal(expectedAccessToken, captureHandlerB.CapturedRequest.Headers.Authorization?.Parameter);
        }
    }
}
