namespace OAuthProxy.AspNetCore.Services.StateManagement
{
    public class AuthorizationStateParameters
    {
        public string? UserId { get; set; }
        public string? RedirectUrl { get; set; }

        public Dictionary<string, string> ExtraParameters { get; set; } = [];
    }
}
