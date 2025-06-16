using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Extensions;

namespace OAuthProxy.AspNetCore.Tests
{
    public class ProxyClientBuilderTest
    {
        private class DummyClient { }

        [Fact]
        public void Constructor_ThrowsArgumentException_WhenServiceProviderNameIsNullOrEmpty()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            Assert.Throws<ArgumentException>(() =>
                new ProxyClientBuilder<DummyClient>("", services, config, "Prefix"));
            Assert.Throws<ArgumentException>(() =>
                new ProxyClientBuilder<DummyClient>(null, services, config, "Prefix"));
        }

        [Fact]
        public void Build_ThrowsInvalidOperationException_WhenNoConfigAvailable()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var builder = new ProxyClientBuilder<DummyClient>("TestProvider", services, config, "Prefix");

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void Build_ThrowsInvalidOperationException_WhenNoAuthorizationFlowBuilder()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                    new[] { new KeyValuePair<string, string?>("Prefix:TestProvider:ClientId", "test-client-id") }).Build();

            var builder = new ProxyClientBuilder<DummyClient>("TestProvider", services, config, "Prefix")
                .WithAuthorizationConfig(config.GetSection("Prefix:TestProvider"));

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void Build_Succeeds_WhenValidConfigurationProvided()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                    new[] { new KeyValuePair<string, string?>("Prefix:TestProvider:ClientId", "test-client-id") }).Build();
            
            var builder = new ProxyClientBuilder<DummyClient>("TestProvider", services, config, "Prefix")
                .WithAuthorizationCodeFlow(config.GetSection("Prefix:TestProvider"));
            
            builder.Build();


        }

    }
}
