namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IProxyClientBuilder : IBuilder
    {
        string ServiceProviderName { get; }

    }
}
