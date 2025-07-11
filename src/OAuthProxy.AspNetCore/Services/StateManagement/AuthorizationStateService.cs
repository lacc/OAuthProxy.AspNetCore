using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("OAuthProxy.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
namespace OAuthProxy.AspNetCore.Services.StateManagement
{
    internal class AuthorizationStateService : IAuthorizationStateService
    {
        private readonly ILogger<AuthorizationStateService> _logger;
        private readonly IUserIdProvider _userIdProvider;
        private readonly IDataProtectionProvider _dpProvider;

        public AuthorizationStateService( ILogger<AuthorizationStateService> logger,
            IUserIdProvider userIdProvider, IDataProtectionProvider dpProvider)
        {
            _logger = logger;
            _userIdProvider = userIdProvider;
            _dpProvider = dpProvider;
        }

        public Task<string> DecorateWithStateAsync(string thirdPartyProvider, string authorizeUrl, AuthorizationStateParameters? parameters = null)
        {
            var userId = _userIdProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated. Cannot generate state.");
            }

            if (parameters == null)
            {
                parameters = new AuthorizationStateParameters();
            }
            if (string.IsNullOrEmpty(parameters.UserId))
            {
                parameters.UserId = userId;
            }

            string? protectedState;
            try
            {
                var protector = _dpProvider.CreateProtector($"OAuthState-{thirdPartyProvider}");
                var stateData = JsonSerializer.Serialize(parameters);
                protectedState = protector.Protect(stateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to protect state data");
                throw new InvalidOperationException("Failed to generate state data.", ex);
            }

            var uri = new UriBuilder(authorizeUrl);
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query) && query.StartsWith('?'))
                query = query[1..];

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(query))
                queryParams.AddRange(query.Split('&').Where(q => !q.StartsWith("state=")));

            queryParams.Add($"state={System.Net.WebUtility.UrlEncode(protectedState)}");
            uri.Query = string.Join("&", queryParams);

            var res = uri.ToString();

            return Task.FromResult( res);
        }
        
        public Task<StateValidationResult> ValidateStateAsync(string thirdPartyProvider, string state)
        {
            AuthorizationStateParameters? stateData;

            try
            {
                var protector = _dpProvider.CreateProtector($"OAuthState-{thirdPartyProvider}");
                var parameters = protector.Unprotect(state);
                stateData = JsonSerializer.Deserialize<AuthorizationStateParameters>(parameters);
            }
            catch( Exception ex)
            {
                _logger.LogError(ex, "Failed to unprotect state data");
                return Task.FromResult(new StateValidationResult
                {
                    ErrorMessage = "Invalid state data."
                });
            }

            if (stateData == null)
            {
                _logger.LogError("State data not found by the data protector");
                return Task.FromResult(new StateValidationResult
                {
                    ErrorMessage = "State data not found."
                });
            }

            if (string.IsNullOrEmpty(stateData.UserId))
            {
                _logger.LogError("State data does not contain UserId");
                return Task.FromResult(new StateValidationResult
                {
                    ErrorMessage = "State data is missing UserId."
                });
            }


            return Task.FromResult(new StateValidationResult
            {
                StateParameters = stateData
                
            });
        }

    }
}
