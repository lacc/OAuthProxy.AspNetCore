namespace OAuthProxy.AspNetCore.Models
{
    public class AuthorizationRequest
    {
        public string ServiceProvider { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class AuthorizationCallback
    {
        public string ServiceProvider { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
    
    public class TokenExchangeResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}
