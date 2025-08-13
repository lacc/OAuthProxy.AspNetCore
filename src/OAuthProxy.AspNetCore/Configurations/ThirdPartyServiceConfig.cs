namespace OAuthProxy.AspNetCore.Configurations
{
    public class ThirdPartyServiceConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string AuthorizeEndpoint { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string Scopes { get; set; } = string.Empty;
        public int? TokenExpirationInDays { get; set; } = null;
    }
}
