using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Models;
using OAuthProxy.AspNetCore.Services;

namespace OAuthProxy.AspNetCore.Tests
{
    public class RefreshTokenServiceTest
    {
        [Fact]
        public async Task RefreshTokenAsync_ReturnsTokenResponse_WhenSuccessful()
        {
            var serviceName = "providerA";
            var refreshToken = "refreshA";
            var config = new ThirdPartyProviderConfig
            {
                ServiceProviderName = serviceName,
                OAuthConfiguration = new ThirdPartyServiceConfig()
            };

            var mockOptions = new Mock<IOptionsSnapshot<ThirdPartyProviderConfig>>();
            mockOptions.Setup(x => x.Get(serviceName)).Returns(config);

            var mockExchanger = new Mock<IOAuthAuthorizationRefreshTokenExchanger>();
            mockExchanger.Setup(x => x.ExchangeRefreshTokenAsync(config.OAuthConfiguration, refreshToken))
                .ReturnsAsync(new TokenResponse
                {
                    AccessToken = "access",
                    RefreshToken = "refresh",
                    ExpiresIn = 3600,
                    TokenType = "Bearer"
                });
            var services = new ServiceCollection();
            services.AddKeyedScoped<IOAuthAuthorizationRefreshTokenExchanger>(
                serviceName, (sp, o) => mockExchanger.Object);
            var mockFactory = new AuthorizationFlowServiceFactory(services.BuildServiceProvider());
            var logger = new Mock<ILogger<RefreshTokenService>>().Object;
            var service = new RefreshTokenService(logger, mockFactory, mockOptions.Object);

            var result = await service.RefreshTokenAsync(serviceName, refreshToken);

            Assert.NotNull(result);
            Assert.Equal("access", result.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.Equal(3600, result.ExpiresIn);
        }

        [Fact]
        public async Task RefreshTokenAsync_Throws_WhenConfigMissing()
        {
            var serviceName = "providerB";
            var refreshToken = "refreshB";
            var mockOptions = new Mock<IOptionsSnapshot<ThirdPartyProviderConfig>>();
            mockOptions.Setup(x => x.Get(serviceName)).Returns((ThirdPartyProviderConfig?)null);

            var mockFactory = new Mock<AuthorizationFlowServiceFactory>(MockBehavior.Strict, new object[] { null! });
            var logger = new Mock<ILogger<RefreshTokenService>>().Object;
            var service = new RefreshTokenService(logger, mockFactory.Object, mockOptions.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RefreshTokenAsync(serviceName, refreshToken));
        }

        [Fact]
        public async Task RefreshTokenAsync_ReturnsNull_WhenExchangerReturnsNull()
        {
            var serviceName = "providerC";
            var refreshToken = "refreshC";
            var config = new ThirdPartyProviderConfig
            {
                ServiceProviderName = serviceName,
                OAuthConfiguration = new ThirdPartyServiceConfig()
            };

            var mockOptions = new Mock<IOptionsSnapshot<ThirdPartyProviderConfig>>();
            mockOptions.Setup(x => x.Get(serviceName)).Returns(config);

            var mockExchanger = new Mock<IOAuthAuthorizationRefreshTokenExchanger>();
            mockExchanger
                .Setup(x => x.ExchangeRefreshTokenAsync(config.OAuthConfiguration, refreshToken))
                .ReturnsAsync((TokenResponse?)null!);

            var services = new ServiceCollection();
            services.AddKeyedScoped<IOAuthAuthorizationRefreshTokenExchanger>(
                serviceName, (sp, o) => mockExchanger.Object);
            
            var mockFactory = new AuthorizationFlowServiceFactory(services.BuildServiceProvider());
            var logger = new Mock<ILogger<RefreshTokenService>>().Object;

            var service = new RefreshTokenService(logger, mockFactory, mockOptions.Object);

            var result = await service.RefreshTokenAsync(serviceName, refreshToken);

            Assert.Null(result);
        }
    }
}