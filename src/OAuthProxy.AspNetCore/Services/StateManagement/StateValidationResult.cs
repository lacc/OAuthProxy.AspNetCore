namespace OAuthProxy.AspNetCore.Services.StateManagement
{
    public class StateValidationResult
    {
        public string? ErrorMessage { get; internal set; }
        public AuthorizationStateParameters? StateParameters { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(StateParameters?.UserId);
    }
}