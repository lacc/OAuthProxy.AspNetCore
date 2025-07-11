using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Tests
{
    public class AuthorizationStateServiceTest
    {
        private static AuthorizationStateService CreateService(
            string userId = "user1")
        {
            //dbContext ??= CreateInMemoryDbContext();
            var logger = new Mock<ILogger<AuthorizationStateService>>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var dataProtectionProvider = DataProtectionProvider.Create("UnitTestPurpose");
            return new AuthorizationStateService(logger.Object, userIdProvider.Object, dataProtectionProvider);
        }

        [Fact]
        public async Task DecorateWithStateAsync_AppendsStateToUrl()
        {
            var service = CreateService("user1");
            var url = "https://example.com/auth?client_id=abc";
            
            var result = await service.DecorateWithStateAsync("providerA", url);

            Assert.Contains("state=", result);
        }

        [Fact]
        public async Task DecorateWithStateAsync_ThrowsIfUserIdMissing()
        {
            var logger = Mock.Of<ILogger<AuthorizationStateService>>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns(null as string);
            var dataProtectionProvider = DataProtectionProvider.Create("UnitTestPurpose");

            var service = new AuthorizationStateService(logger, userIdProvider.Object, dataProtectionProvider);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DecorateWithStateAsync("providerA", "https://example.com"));
        }

        [Fact]
        public async Task ValidateStateAsync_ReturnsUserId_OnValidState()
        {
            var service = CreateService("user1");
            var url = "https://example.com/auth?client_id=abc";
            var provider = "providerA";

            var decoratedUrl = await service.DecorateWithStateAsync(provider, url);
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(decoratedUrl).Query)["state"];

            Assert.NotNull(state);
            var result = await service.ValidateStateAsync(provider, state);

            Assert.True(result.IsValid);
            Assert.NotNull(result?.StateParameters);
            Assert.Equal("user1", result.StateParameters.UserId);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateStateAsync_ReturnsError_OnInvalidFormat()
        {
            var service = CreateService("user1");

            var result = await service.ValidateStateAsync("providerA", "badstateformat");

            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Invalid state", result.ErrorMessage);
        }

        [Fact]
        public async Task DecorateWithStateAsync_AppendsProtectedState_And_CanBeValidated()
        {
            // Arrange
            var userId = "test-user";

            var logger = new Mock<ILogger<AuthorizationStateService>>();
            var userIdProvider = new Mock<IUserIdProvider>();
            userIdProvider.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var dataProtectionProvider = DataProtectionProvider.Create("UnitTestPurpose");
            var service = new AuthorizationStateService(
                logger.Object, userIdProvider.Object, dataProtectionProvider);

            var url = "https://example.com/auth?client_id=abc";
            var redirectUrl = "https://example.com/redirect";

            // Act
            var resultUrl = await service.DecorateWithStateAsync("providerA", url, new AuthorizationStateParameters
            {
                RedirectUrl = redirectUrl
            });
            Assert.Contains("state=", resultUrl);

            // Extract state from URL
            var state = System.Web.HttpUtility.ParseQueryString(new Uri(resultUrl).Query)["state"];
            Assert.False(string.IsNullOrEmpty(state));

            // Validate state
            var validation = await service.ValidateStateAsync("providerA", state!);
            Assert.NotNull(validation);
            Assert.Null(validation.ErrorMessage);
            Assert.NotNull(validation.StateParameters);
            Assert.Equal(userId, validation.StateParameters.UserId);
            Assert.Equal(redirectUrl, validation.StateParameters.RedirectUrl);
        }
    }
}
