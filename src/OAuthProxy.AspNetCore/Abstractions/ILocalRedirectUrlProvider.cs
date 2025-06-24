
namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface ILocalRedirectUrlProvider
    {
        string GetPersistedUri(string authState);
        Task<string> GetPersistedUriAsync(string authState);
        Task PersistUriAsync(string authState, string uri);
    }
}
