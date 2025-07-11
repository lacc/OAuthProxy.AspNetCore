
namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface ILocalRedirectUrlProvider
    {
        Task<string> GetPersistedUriAsync(string authState, bool deleteAfterGet = true);
        Task PersistUriAsync(string authState, string uri);
    }
}
