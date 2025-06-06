using Microsoft.Extensions.Logging;

namespace OAuthProxy.AspNetCore.Services
{
    public class OAuthService
    {
        private readonly ThirdPartyServiceFactory _serviceFactory;
        private readonly TokenStorageService _tokenStorageService;
        private readonly ILogger<OAuthService> _logger;

        public OAuthService(ThirdPartyServiceFactory serviceFactory, TokenStorageService tokenStorageService, ILogger<OAuthService> logger)
        {
            _serviceFactory = serviceFactory;
            _tokenStorageService = tokenStorageService;
            _logger = logger;
        }

        public async Task<string> GetAuthorizeUrlAsync(string serviceName, string redirectUri)
        {
            var service = _serviceFactory.GetService(serviceName);
            return await service.GetAuthorizeUrlAsync(redirectUri);
        }

        public async Task<bool> HandleCallbackAsync(string userId, string serviceName, string code, string redirectUri)
        {
            var service = _serviceFactory.GetService(serviceName);
            var (accessToken, refreshToken, expiry) = await service.ExchangeCodeAsync(code, redirectUri);
            await _tokenStorageService.SaveTokenAsync(userId, serviceName, accessToken, refreshToken, expiry);
            return true;
        }

        public async Task<string> GetValidAccessTokenAsync(string userId, string serviceName)
        {
            var token = await _tokenStorageService.GetTokenAsync(userId, serviceName);
            if (token == null)
                throw new InvalidOperationException("No token found for the user and service.");

            if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(-5))
            {
                var service = _serviceFactory.GetService(serviceName);
                if (string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogWarning("Access token expired and no refresh token available for user {UserId} and service {ServiceName}.", userId, serviceName);
                    throw new InvalidOperationException("Access token expired and no refresh token available.");
                }
                   
                var (newAccessToken, newRefreshToken, newExpiry) = await service.RefreshAccessTokenAsync(token.RefreshToken);
                await _tokenStorageService.SaveTokenAsync(userId, serviceName, newAccessToken, newRefreshToken, newExpiry);
                return newAccessToken;
            }

            return token.AccessToken;
        }
    }
}
