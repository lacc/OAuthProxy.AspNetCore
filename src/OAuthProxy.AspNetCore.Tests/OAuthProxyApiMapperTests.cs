using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Configurations;
using Xunit;

namespace OAuthProxy.AspNetCore.Tests
{
    public class OAuthProxyApiMapperTests
    {
        [Fact]
        public void Prefix_Is_Normalized_With_Leading_Slash()
        {
            // Arrange

            var proxyConfig = new OAuthProxyConfiguration();
            proxyConfig.ApiMapperConfiguration.ProxyUrlPrefix = "api/oauth";

            var services = new ServiceCollection();
            services.AddSingleton(proxyConfig);
            services.AddSingleton<IRegisteredProxyProviders>(Mock.Of<IRegisteredProxyProviders>(p => p.ServiceProviderName == "TestProvider"));
            services.AddSingleton<IProxyApiMapper>(Mock.Of<IProxyApiMapper>(m => m.ServiceProviderName == "TestProvider"));
            var provider = services.BuildServiceProvider();

            var app = new Mock<IEndpointRouteBuilder>();
            app.SetupGet(a => a.ServiceProvider).Returns(provider);

            // Act
            OAuthProxyApiMapper.MapProxyClientEndpoints(app.Object);

            // Assert if the mapping was created with the correct prefix
            app.Verify(a => a.MapGroup("/api/oauth"), Times.Once());

        }

        [Fact]
        public void GenericApiEndpoints_NotMapped_When_MapGenericApi_False()
        {
            // Arrange
            
            var proxyConfig = new OAuthProxyConfiguration();

            proxyConfig.ApiMapperConfiguration.ProxyUrlPrefix = "/api/proxy";
            proxyConfig.ApiMapperConfiguration.MapGenericApi = false;

            var services = new ServiceCollection();
            services.AddSingleton(proxyConfig);
            services.AddSingleton<IRegisteredProxyProviders>(Mock.Of<IRegisteredProxyProviders>(p => p.ServiceProviderName == "TestProvider"));
            services.AddSingleton<IProxyApiMapper>(Mock.Of<IProxyApiMapper>(m => m.ServiceProviderName == "TestProvider"));
            var provider = services.BuildServiceProvider();

            var routeGroupBuilderMock = new Mock<RouteGroupBuilder>(MockBehavior.Loose, new object[] { new Mock<IEndpointRouteBuilder>().Object });
            var app = new Mock<IEndpointRouteBuilder>();
            app.SetupGet(a => a.ServiceProvider).Returns(provider);
            app.Setup(a => a.MapGroup(It.IsAny<string>())).Returns(routeGroupBuilderMock.Object);

            // Act
            OAuthProxyApiMapper.MapProxyClientEndpoints(app.Object);

            // Assert
            // Since MapGenericApi is false, MapGet should not be called for generic endpoints
            routeGroupBuilderMock.Verify(g => g.MapGet(It.Is<string>(s => s == "{endpoint}"), It.IsAny<Delegate>()), Times.Never());
        }
    }
}