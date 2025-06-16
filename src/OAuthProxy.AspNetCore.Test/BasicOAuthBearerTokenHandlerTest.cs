using Moq;
using Moq.Protected;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Handlers;
using OAuthProxy.AspNetCore.Models;
using System.Net;

namespace OAuthProxy.AspNetCore.Test
{
    public class BasicOAuthBearerTokenHandlerTest
    {
        private static HttpMessageInvoker CreateInvoker(
            ITokenStorageService tokenService,
            IUserIdProvider userIdProvider,
            IProxyRequestContext proxyRequestContext,
            HttpResponseMessage? innerResponse = null)
        {
            var innerHandler = new Mock<HttpMessageHandler>();
            innerHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(innerResponse ?? new HttpResponseMessage(HttpStatusCode.OK));

            var handler = new BasicOAuthBearerTokenHandler(tokenService, userIdProvider, proxyRequestContext)
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
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns((string)null);
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
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns((string)null);

            var invoker = CreateInvoker(tokenService, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfTokenMissing()
        {
            var tokenService = new Mock<ITokenStorageService>();
            tokenService.Setup(x => x.GetTokenAsync("user1", "serviceA")).ReturnsAsync((UserTokenDTO)null);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfTokenExpiredAndNoRefreshToken()
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

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_RefreshesToken_IfExpiredAndRefreshTokenAvailable()
        {
            var expiredToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = "expired",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var refreshedToken = new UserTokenDTO
            {
                ServiceName = "serviceA",
                UserId = "user1",
                AccessToken = "newtoken",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            var tokenService = new Mock<ITokenStorageService>();
            
            tokenService.Setup(x => x.GetTokenAsync(It.Is<string>(s => s == "user1"), It.Is<string>(s => s == "serviceA"))).ReturnsAsync(expiredToken);
            tokenService.Setup(x => x.RefreshTokenAsync(It.Is<string>(s => s == "user1"), It.Is<string>(s => s == "serviceA"), It.Is<string>(s => s == "refresh"))).ReturnsAsync(refreshedToken);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://test");
            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("newtoken", request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_IfRefreshFails()
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
            tokenService.Setup(x => x.RefreshTokenAsync("user1", "serviceA", "refresh")).ReturnsAsync((UserTokenDTO)null);
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ReturnsInternalServerError_IfRefreshThrows()
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
            tokenService.Setup(x => x.RefreshTokenAsync("user1", "serviceA", "refresh")).ThrowsAsync(new Exception("fail"));
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns("user1");
            var proxyRequestContext = new Mock<IProxyRequestContext>();
            proxyRequestContext.Setup(x => x.GetServiceName()).Returns("serviceA");

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
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

            var invoker = CreateInvoker(tokenService.Object, userIdProvider.Object, proxyRequestContext.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://test");
            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("validtoken", request.Headers.Authorization.Parameter);
        }
    }
}
