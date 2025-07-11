using Microsoft.EntityFrameworkCore;
using Moq;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Services;

namespace OAuthProxy.AspNetCore.Tests
{
    public class TokenStorageServiceTest
    {
        private static TokenDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TokenDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TokenDbContext(options);
        }

        [Fact]
        public async Task SaveTokenAsync_AddsAndUpdatesToken()
        {
            var db = CreateInMemoryDbContext();
            var service = new TokenStorageService(db, new Mock<IRefreshTokenService>().Object);
            var userId = "user1";
            var serviceName = "providerA";
            var accessToken = "token1";
            var refreshToken = "refresh1";
            var expiry = DateTime.UtcNow.AddHours(1);

            // Add new token
            await service.SaveTokenAsync(userId, serviceName, accessToken, refreshToken, expiry);
            var token = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName);
            Assert.NotNull(token);
            Assert.Equal(accessToken, token.AccessToken);
            Assert.Equal(refreshToken, token.RefreshToken);
            Assert.Equal(expiry, token.ExpiresAt);

            // Update existing token
            var newAccessToken = "token2";
            var newRefreshToken = "refresh2";
            var newExpiry = DateTime.UtcNow.AddHours(2);
            await service.SaveTokenAsync(userId, serviceName, newAccessToken, newRefreshToken, newExpiry);
            var updatedToken = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName);
            Assert.NotNull(updatedToken);
            Assert.Equal(newAccessToken, updatedToken.AccessToken);
            Assert.Equal(newRefreshToken, updatedToken.RefreshToken);
            Assert.Equal(newExpiry, updatedToken.ExpiresAt);
        }

        [Fact]
        public async Task GetTokenAsync_ReturnsUserTokenDTO_IfExists()
        {
            var db = CreateInMemoryDbContext();
            var service = new TokenStorageService(db, new Mock<IRefreshTokenService>().Object);
            var userId = "user2";
            var serviceName = "providerB";
            var accessToken = "tokenX";
            var refreshToken = "refreshX";
            var expiry = DateTime.UtcNow.AddHours(1);

            await service.SaveTokenAsync(userId, serviceName, accessToken, refreshToken, expiry);
            var dto = await service.GetTokenAsync(userId, serviceName);

            Assert.NotNull(dto);
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(serviceName, dto.ServiceName);
            Assert.Equal(accessToken, dto.AccessToken);
            Assert.Equal(refreshToken, dto.RefreshToken);
            Assert.Equal(expiry, dto.ExpiresAt);
        }

        [Fact]
        public async Task GetTokenAsync_ReturnsNull_IfNotExists()
        {
            var db = CreateInMemoryDbContext();
            var service = new TokenStorageService(db, new Mock<IRefreshTokenService>().Object);
            var dto = await service.GetTokenAsync("no-user", "no-service");
            Assert.Null(dto);
        }

        [Fact]
        public async Task DeleteTokenAsync_RemovesToken()
        {
            var db = CreateInMemoryDbContext();
            var service = new TokenStorageService(db, new Mock<IRefreshTokenService>().Object);
            var userId = "user3";
            var serviceName = "providerC";
            var accessToken = "tokenY";
            var refreshToken = "refreshY";
            var expiry = DateTime.UtcNow.AddHours(1);

            await service.SaveTokenAsync(userId, serviceName, accessToken, refreshToken, expiry);
            Assert.NotNull(await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName));

            await service.DeleteTokenAsync(userId, serviceName);
            Assert.Null(await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName));
        }

        [Fact]
        public async Task GetConnectedServicesAsync_ReturnsActiveServices()
        {
            var db = CreateInMemoryDbContext();
            var service = new TokenStorageService(db, new Mock<IRefreshTokenService>().Object);
            var userId = "user4";
            var now = DateTime.UtcNow;

            await service.SaveTokenAsync(userId, "service1", "a", "b", now.AddHours(1));
            await service.SaveTokenAsync(userId, "service2", "a", "b", now.AddHours(-1)); // expired
            await service.SaveTokenAsync(userId, "service3", "a", "b", now.AddHours(2));

            var services = await service.GetConnectedServicesAsync(userId);
            Assert.Contains("service1", services);
            Assert.Contains("service3", services);
            Assert.DoesNotContain("service2", services);
        }
    }
}
