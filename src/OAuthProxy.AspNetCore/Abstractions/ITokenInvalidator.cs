namespace OAuthProxy.AspNetCore.Abstractions
{
    public interface ITokenInvalidator
    {
        Task InvalidateTokenAsync(string userId, string serviceName);
    }
}
