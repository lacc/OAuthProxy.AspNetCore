using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Models;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.AuthorizationCodeFlow;
using OAuthProxy.AspNetCore.Services.ClientCredentialsFlow;
using OAuthProxy.AspNetCore.Services.StateManagement;
using System;
using System.Net;
using System.Net.Http.Json;

namespace OAuthProxy.AspNetCore.Tests
{
    public class BasicOAuthBearerTokenHandler_ClientCredentialsFlowTest
    {
        private static HttpMessageInvoker CreateInvoker(
            string serviceProviderName,
            ITokenStorageService tokenService,
            IUserIdProvider userIdProvider,
            IProxyRequestContext proxyRequestContext,
            HttpResponseMessage? innerResponse = null,
            bool flowThrowsError = false)
        {
            var innerHandler = new Mock<HttpMessageHandler>();
            innerHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(innerResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
            var logger = new Mock<ILogger<BasicOAuthBearerTokenHandler>>();

            var accessTokenBuilder = new Mock<IAccessTokenBuilder>();

            IServiceCollection services = new ServiceCollection();
            services.AddScoped<ITokenStorageService>(_ => tokenService);
            services.AddScoped<IUserIdProvider>(_ => userIdProvider);
            services.AddKeyedScoped<IProxyRequestContext>(serviceProviderName, (sp, o) => proxyRequestContext);
            services.AddScoped<IAccessTokenBuilder>(_ => accessTokenBuilder.Object);
            services.AddKeyedScoped<IAccessTokenBuilder, ClientCredentialsAccessTokenBuilder>(serviceProviderName);
            if (!flowThrowsError)
            {
                services.AddKeyedScoped<IClientCredentialsTokenExchanger, ClientCredentialsFlowExchangeToken>(serviceProviderName);
            }
            else
            {
                var mockedExcchanger = new Mock<IClientCredentialsTokenExchanger>();
                mockedExcchanger
                    .Setup(x => x.ExchangeTokenAsync(It.IsAny<ThirdPartyServiceConfig>()))
                    .ThrowsAsync(new Exception("Exchange failed"));

                services.AddKeyedScoped(serviceProviderName, (sp, o) => mockedExcchanger.Object);
            }
                
            services.AddLogging();
            services.AddScoped<AuthorizationFlowServiceFactory>();

            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"accessToken\":\"validtoken\"}")
            };
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            services.AddHttpClient("")
                .ConfigurePrimaryHttpMessageHandler(
                    () => mockHandler.Object
                );

            services.Configure<ThirdPartyProviderConfig>(serviceProviderName, options =>
            {
                options.ServiceProviderName = serviceProviderName;
                options.OAuthConfiguration = new ThirdPartyServiceConfig()
                {
                    ApiBaseUrl = "https://api.example.com",
                    ClientId = "client-id",
                    ClientSecret = "client-secret",
                    TokenEndpoint = "https://api.example.com/token",
                };
            });



            var serviceProvider = services.BuildServiceProvider();

            var authorizationFlowServiceFactory = serviceProvider.GetRequiredService<AuthorizationFlowServiceFactory>();

            var handler = new BasicOAuthBearerTokenHandler(userIdProvider, proxyRequestContext, authorizationFlowServiceFactory, logger.Object)
            {
                InnerHandler = innerHandler.Object
            };
            return new HttpMessageInvoker(handler);
        }


        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfTokenMissing()
        {
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync((UserTokenDTO?)null);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker("serviceA", tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfTokenExpired()
        {
            var expiredToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = "expired",
                RefreshToken = null,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync(expiredToken);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker("serviceA", tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfExchangeFails()
        {
            var expiredToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = "expired",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync(expiredToken);
            tokenService.Setup(x => x.RefreshTokenAsync("user1", "serviceA", "refresh")).ReturnsAsync((UserTokenDTO?)null);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker("serviceA", tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsInternalServerError_IfExchangeThrows()
        {
            var expiredToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = ""
            };
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync(expiredToken);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker("serviceA", tokenService.Object, userIdProvider.Object, proxyRequestContext.Object, flowThrowsError: true);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_SetsBearerHeader_IfTokenValid()
        {
            var validToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = "validtoken",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync(validToken);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker("serviceA", tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://test");
            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("validtoken", request.Headers.Authorization.Parameter);
        }
    }
}
