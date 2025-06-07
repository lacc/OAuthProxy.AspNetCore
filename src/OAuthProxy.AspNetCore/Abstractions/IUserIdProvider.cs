using Microsoft.AspNetCore.Http;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface IUserIdProvider
    {
        string? GetCurrentUserId();
    }
}
