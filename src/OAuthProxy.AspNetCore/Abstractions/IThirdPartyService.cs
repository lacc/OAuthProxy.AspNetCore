namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IThirdPartyService
    {
        string ServiceName { get; }
        Task<string> GetAuthorizeUrlAsync(string redirectUri);
        Task<(string AccessToken, string RefreshToken, DateTime Expiry)> ExchangeCodeAsync(string code, string redirectUri);
        Task<(string AccessToken, string RefreshToken, DateTime Expiry)> RefreshAccessTokenAsync(string refreshToken);
        Task<HttpResponseMessage> CallEndpointAsync(string endpoint, string accessToken, HttpMethod method, object? data = null);

    }
}
