using OAuthProxy.AspNetCore.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface ITokenStorageService
    {
        Task<List<string>> GetConnectedServicesAsync(string userId);
        Task<UserTokenDTO?> GetTokenAsync(string userId, string serviceName);
        Task<UserTokenDTO?> RefreshTokenAsync(string userId, string serviceName, string refreshToken);
        Task SaveTokenAsync(string userId, string serviceName, string accessToken, string refreshToken, DateTime expiry);
    }
}
