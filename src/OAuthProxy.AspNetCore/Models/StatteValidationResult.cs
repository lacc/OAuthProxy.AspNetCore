using OAuthProxy.AspNetCore.Services.StateManagement;

namespace OAuthProxy.AspNetCore.Models
{
    public class StatteValidationResult
    {
        public string? ErrorMessage { get; internal set; }
        public AuthorizationStateParameters? StateParameters { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(StateParameters?.UserId);
    }
}