namespace OAuthProxy.AspNetCore.Models
{
    public class StatteValidationResult
    {
        public string? ErrorMessage { get; internal set; }
        public string? UserId { get; internal set; }
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(UserId);
    }
}