using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IAuthorizationStateService
    {
        Task<string> DecorateWithStateAsync(string thirdPartyProvider, string authorizeUrl, AuthorizationStateParameters? parameters = null);
        Task<StateValidationResult> ValidateStateAsync(string thirdPartyProvider, string state);
    }
}