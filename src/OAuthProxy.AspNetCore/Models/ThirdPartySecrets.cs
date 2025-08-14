namespace OAuthProxy.AspNetCore.Models
{
    public class ThirdPartySecrets
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
