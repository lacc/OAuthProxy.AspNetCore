using OAuthProxy.AspNetCore.Models;
using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IAuthorizationStateService
    {
        Task<string> DecorateWithStateAsync(string thirdPartyProvider, string authorizeUrl, AuthorizationStateParameters? parameters = null);
        Task<StatteValidationResult> ValidateStateAsync(string thirPartyProvider, string state);
    }
}