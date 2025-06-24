
namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface ILocalRedirectUrlProvider
    {
        string GetPersistedUri(string authState, bool deleteAfterGet = true);
        Task<string> GetPersistedUriAsync(string authState, bool deleteAfterGet = true);
        Task PersistUriAsync(string authState, string uri);
    }
}
