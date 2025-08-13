using System.Net;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public record struct AccessTokenBuilderResponse(string? AccessToken, string? ErrorMessage, HttpStatusCode StatusCode = HttpStatusCode.OK);
    public interface IAccessTokenBuilder
    {
        Task<AccessTokenBuilderResponse> BuildAccessTokenAsync(HttpRequestMessage request, string userId, string serviceName);
    }
}
