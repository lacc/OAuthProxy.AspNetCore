using OAuthProxy.AspNetCore.Models;

namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IAuthorizationStateService
    {
        Task<string> DecorateWithStateAsync(string thirdPartyProvider, string authorizeUrl);
        void EnsureValidState(string serviceProviderName, string state);
        Task<StatteValidationResult> ValidateStateAsync(string thirPartyProvider, string state);
    }
}