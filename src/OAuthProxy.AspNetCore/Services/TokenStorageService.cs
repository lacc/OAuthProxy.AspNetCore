using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Services
{
    public class TokenStorageService : ITokenStorageService
    {
        private readonly TokenDbContext _dbContext;
        private readonly IRefreshTokenService _refreshTokenService;

        public TokenStorageService(TokenDbContext dbContext, IRefreshTokenService refreshTokenService)
        {
            _dbContext = dbContext;
            _refreshTokenService = refreshTokenService;
        }

        public async Task SaveTokenAsync(string userId, string serviceName, string accessToken, string refreshToken, DateTime expiry)
        {
            var token = await _dbContext.OAuthTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName);

            if (token == null)
            {
                token = new ThirdPartyTokenEntity { UserId = userId, ThirdPartyServiceProvider = serviceName, AccessToken = accessToken };
                _dbContext.OAuthTokens.Add(token);
            }

            token.AccessToken = accessToken;
            token.RefreshToken = refreshToken;
            token.ExpiresAt = expiry;
            token.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserTokenDTO?> GetTokenAsync(string userId, string serviceName)
        {
            var token = await _dbContext.OAuthTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName);

            return token == null ? null : new UserTokenDTO
            {
                Id = token.Id,
                UserId = token.UserId,
                ServiceName = token.ThirdPartyServiceProvider,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                CreatedAt = token.CreatedAt,
                UpdatedAt = token.UpdatedAt
            };
        }

        public async Task DeleteTokenAsync(string userId, string serviceName)
        {
            var token = await _dbContext.OAuthTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.ThirdPartyServiceProvider == serviceName);

            if (token != null)
            {
                _dbContext.OAuthTokens.Remove(token);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetConnectedServicesAsync(string userId)
        {
            return await _dbContext.OAuthTokens
                .Where(t => t.UserId == userId && t.ExpiresAt > DateTime.UtcNow)
                .Select(t => t.ThirdPartyServiceProvider)
                .ToListAsync();
        }

        public async Task<UserTokenDTO?> RefreshTokenAsync(string userId, string serviceName, string refreshToken)
        {
            var refreshedToken = await _refreshTokenService.RefreshTokenAsync(
                serviceName, refreshToken);

            if (refreshedToken == null)
            {
                return null;
            }

            await SaveTokenAsync(userId, serviceName,
                refreshedToken.AccessToken, refreshedToken.RefreshToken ?? string.Empty, DateTime.UtcNow.AddSeconds(refreshedToken.ExpiresIn));

            return await GetTokenAsync(userId, serviceName);
        }
    }
}
