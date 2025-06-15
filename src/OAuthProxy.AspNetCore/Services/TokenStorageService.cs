using Microsoft.EntityFrameworkCore;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Services
{
    public class TokenStorageService
    {
        private readonly TokenDbContext _dbContext;

        public TokenStorageService(TokenDbContext dbContext)
        {
            _dbContext = dbContext;
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

        internal async Task<UserTokenDTO> RefreshTokenAsync(string userId, string serviceName, string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}
