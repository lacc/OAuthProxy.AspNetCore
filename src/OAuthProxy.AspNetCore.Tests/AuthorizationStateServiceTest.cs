using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Services;

namespace OAuthProxy.AspNetCore.Tests
{
    public class AuthorizationStateServiceTest
    {
        const char StateSeparator = '.';
        private static TokenDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TokenDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TokenDbContext(options);
        }

        private static AuthorizationStateService CreateService(
            TokenDbContext dbContext,
            string userId = "user1")
        {
            dbContext ??= CreateInMemoryDbContext();
            var logger = new Mock<ILogger<AuthorizationStateService>>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns(userId);
            return new AuthorizationStateService(dbContext, logger.Object, userIdProvider.Object);
        }

        [Fact]
        public async Task DecorateWithStateAsync_AppendsStateToUrl()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");
            var url = "https://example.com/auth?client_id=abc";
            
            var result = await service.DecorateWithStateAsync("providerA", url);

            Assert.Contains("state=", result);
        }

        [Fact]
        public async Task DecorateWithStateAsync_ThrowsIfUserIdMissing()
        {
            var db = CreateInMemoryDbContext();
            var logger = Mock.Of<ILogger<AuthorizationStateService>>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns(null as string);
            var service = new AuthorizationStateService(db, logger, userIdProvider.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DecorateWithStateAsync("providerA", "https://example.com"));
        }

        [Fact]
        public async Task ValidateStateAsync_ReturnsUserId_OnValidState()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");
            var url = "https://example.com/auth?client_id=abc";
            var provider = "providerA";

            var decoratedUrl = await service.DecorateWithStateAsync(provider, url);
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(decoratedUrl).Query)["state"];

            Assert.NotNull(state);
            var result = await service.ValidateStateAsync(provider, state);

            Assert.NotNull(result);
            Assert.Equal("user1", result.UserId);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateStateAsync_ReturnsError_OnInvalidFormat()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");

            var result = await service.ValidateStateAsync("providerA", "badstateformat");

            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Invalid state format", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateStateAsync_ReturnsError_OnNotFound()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");

            // Generate a valid-looking state, but not in DB
            var fakeState = $"id{StateSeparator}1234567890{StateSeparator}fakehmac";
            var result = await service.ValidateStateAsync("providerA", fakeState);

            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValidState_Throws_OnNullOrEmpty()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");
            Assert.Throws<ArgumentException>(() => service.EnsureValidState("providerA", ""));
        }

        [Fact]
        public void EnsureValidState_Throws_OnInvalidFormat()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");

            Assert.Throws<ArgumentException>(() => service.EnsureValidState("providerA", "badstate"));
        }

        [Fact]
        public void EnsureValidState_Throws_OnExpiredState()
        {
            var db = CreateInMemoryDbContext();
            var service = CreateService(db, "user1");

            // expired state: expiresAt in the past
            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds();
            var state = $"id{StateSeparator}{expiredAt}{StateSeparator}hmac";
            Assert.Throws<InvalidOperationException>(() => service.EnsureValidState("providerA", state));
        }
    }
}
