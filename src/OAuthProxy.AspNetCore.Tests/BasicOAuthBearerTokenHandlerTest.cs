using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Models;
using OAuthProxy.AspNetCore.Services;
using OAuthProxy.AspNetCore.Services.StateManagement;
using System.Net;

namespace OAuthProxy.AspNetCore.Tests
{
    public class BasicOAuthBearerTokenHandlerTest
    {
        private static HttpMessageInvoker CreateInvoker(
            ITokenStorageService tokenService,
            IUserIdProvider userIdProvider,
            IProxyRequestContext proxyRequestContext,
            AuthorizationFlowServiceFactory authorizationFlowServiceFactory = null,
            HttpResponseMessage? innerResponse = null)
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
            services.AddScoped<IProxyRequestContext>(_ => proxyRequestContext);
            services.AddScoped<IAccessTokenBuilder>(_ => accessTokenBuilder.Object);

            var _authorizationFlowServiceFactory = authorizationFlowServiceFactory;
            if (_authorizationFlowServiceFactory == null)
            {
                _authorizationFlowServiceFactory = new AuthorizationFlowServiceFactory(services.BuildServiceProvider());
            }

            var handler = new BasicOAuthBearerTokenHandler(userIdProvider, proxyRequestContext, _authorizationFlowServiceFactory, logger.Object)
            {
                InnerHandler = innerHandler.Object
            };
            return new HttpMessageInvoker(handler);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfUserIdMissing()
        {
            var tokenService = Mock.Of<ITokenStorageService>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
            var proxyRequestContext = Mock.Of<IProxyRequestContext>();

            var invoker = CreateInvoker(tokenService, userIdProvider.Object, proxyRequestContext);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsBadRequest_IfServiceNameMissing()
        {
            var tokenService = Mock.Of<ITokenStorageService>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns(string.Empty);

            var invoker = CreateInvoker(tokenService, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

    }
}
