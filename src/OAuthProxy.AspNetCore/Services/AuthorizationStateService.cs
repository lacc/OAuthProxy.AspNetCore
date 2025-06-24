using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OAuthProxy.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
namespace OAuthProxy.AspNetCore.Services
{
    internal class AuthorizationStateService : IAuthorizationStateService
    {
        const int ClockSkew = 60;
        const int StateExpirationMinutes = 10;
        const char StateSeparator = '.';
        private readonly TokenDbContext _dbContext;
        private readonly ILogger<AuthorizationStateService> _logger;
        private readonly IUserIdProvider _userIdProvider;

        public AuthorizationStateService(TokenDbContext dbContext, ILogger<AuthorizationStateService> logger, IUserIdProvider userIdProvider)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userIdProvider = userIdProvider;
        }

        public async Task<string> DecorateWithStateAsync(string thirdPartyProvider, string authorizeUrl)
        {
            var userId = _userIdProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated. Cannot generate state.");
            }

            var state = await GenerateStateAsync(userId, thirdPartyProvider);

            // Manually append the state parameter without encoding
            var uri = new UriBuilder(authorizeUrl);
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query) && query.StartsWith('?'))
                query = query[1..];

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(query))
                queryParams.AddRange(query.Split('&').Where(q => !q.StartsWith("state=")));

            queryParams.Add($"state={System.Net.WebUtility.UrlEncode(state)}");
            uri.Query = string.Join("&", queryParams);

            var res = uri.ToString();

            return res;
        }
        private async Task<string> GenerateStateAsync(string userId, string thirPartyProvider)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(thirPartyProvider))
            {
                throw new ArgumentException("UserId, ThirdPartyProvider and StateSecret must not be null or empty.");
            }

            _dbContext.OAuthStates.RemoveRange(_dbContext.OAuthStates.Where(s => s.UserId == userId && s.ThirdPartyServiceProvider == thirPartyProvider));

            var stateSecret = GenerateStateSecret();

            var stateId = CalculateState(stateSecret, out long expiresAt, out string state);

            await _dbContext.OAuthStates.AddAsync(new StateEntity
            {
                StateId = stateId,
                UserId = userId,
                ThirdPartyServiceProvider = thirPartyProvider,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt).UtcDateTime,
                StateSecret = stateSecret
            });
            await _dbContext.SaveChangesAsync();

            return state;
        }

        private static string GenerateStateSecret()
        {
            // Generate a secure random state secret using RandomNumberGenerator
            var secretBytes = new byte[32]; // 256 bits
            System.Security.Cryptography.RandomNumberGenerator.Fill(secretBytes);
            return Convert.ToBase64String(secretBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string CalculateState(string stateSecret, out long expiresAt, out string state)
        {
            var stateId = Guid.NewGuid().ToString(); // Prevent CSRF
            var nonce = Guid.NewGuid().ToString(); // For security

            expiresAt = DateTimeOffset.UtcNow.AddMinutes(StateExpirationMinutes).ToUnixTimeSeconds();
            var stateData = $"{stateId}{StateSeparator}{expiresAt}";

            var hmac = ComputeHmac(stateData, stateSecret);
            state = $"{stateData}{StateSeparator}{hmac}";
            // Format: stateId.hmac

            return stateId;
        }

        public async Task<StatteValidationResult> ValidateStateAsync(string thirPartyProvider, string state)
        {
            //string stateSecret 
            var parts = state.Split(StateSeparator);
            if (parts.Length != 3)
            {
                _logger.LogError("Invalid state format");
                return new StatteValidationResult
                {
                    ErrorMessage = "Invalid state format. Expected format: stateId.expiresAt.hmac"
                };
            }

            var stateId = parts[0];
            var expiresAtStr = parts[1];
            var providedHmac = parts[2];

            var stateEntity = await _dbContext.OAuthStates
                .FirstOrDefaultAsync(s => s.ThirdPartyServiceProvider == thirPartyProvider && s.StateId == stateId);

            if (stateEntity == null)
            {
                _logger.LogError("State not found in the database for provider: {Provider}", thirPartyProvider);
                return new StatteValidationResult
                {
                    ErrorMessage = "State not found or has already been used."
                };
            }

            var stateSecret = stateEntity.StateSecret;

            // Verify HMAC
            var stateData = $"{stateId}{StateSeparator}{expiresAtStr}";
            var expectedHmac = ComputeHmac(stateData, stateSecret);
            if (providedHmac != expectedHmac)
            {
                _logger.LogError("Invalid state HMAC");
                return new StatteValidationResult
                {
                    ErrorMessage = "Invalid state HMAC. Possible tampering detected."
                };
            }

            // Verify expiration
            if (!long.TryParse(expiresAtStr, out var expiresAt) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt + ClockSkew) // Allow 60s clock skew
            {
                _logger.LogError("State has expired or invalid expiresAt");
                return new StatteValidationResult
                {
                    ErrorMessage = "State has expired or is invalid."
                };
            }

            _logger.LogInformation("State validated successfully for stateId: {StateId}", stateId);


            // Remove the state from the store to prevent reuse
            _dbContext.OAuthStates.Remove(stateEntity);
            await _dbContext.SaveChangesAsync();

            return new StatteValidationResult
            {
                UserId = stateEntity.UserId
            };
        }

        private static string ComputeHmac(string data, string key)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public void EnsureValidState(string serviceProviderName, string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ArgumentException("State cannot be null or empty.", nameof(state));
            }
            var parts = state.Split(StateSeparator);
            if (parts.Length != 3)
            {
                throw new ArgumentException("Invalid state format. Expected format: stateId.expiresAt.hmac", nameof(state));
            }
            var stateId = parts[0];
            var expiresAtStr = parts[1];
            if (!long.TryParse(expiresAtStr, out var expiresAt) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt + ClockSkew)
            {
                throw new InvalidOperationException("State has expired or is invalid.");
            }
            // Additional validation logic can be added here if needed

        }
    }
}
