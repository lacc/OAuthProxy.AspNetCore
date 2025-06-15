namespace OAuthProxy.AspNetCore.Abstractions
{
    internal interface IProxyRequestContext
    {
        void SetServiceName(string serviceName);
        string GetServiceName();
    }
}
