namespace OAuthProxy.AspNetCore.Models
{
    /// <summary>
    /// Represents secrets and credentials required to authenticate with a third-party service.
    /// This model typically contains values such as client ID, client secret, and API key.
    /// </summary>
    public class ThirdPartySecrets
    {
        /// <summary>
        /// The client identifier used to authenticate with the third-party service.
        /// </summary>
        public string ClientId { get; init; } = string.Empty;
        /// <summary>
        /// The client secret used to authenticate with the third-party service.
        /// </summary>
        public string ClientSecret { get; init; } = string.Empty;
        /// <summary>
        /// The API key used to access the third-party service.
        /// </summary>
        public string ApiKey { get; init; } = string.Empty;
    }
}
