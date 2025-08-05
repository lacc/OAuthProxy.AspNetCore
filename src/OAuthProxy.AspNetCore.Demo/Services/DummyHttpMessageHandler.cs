using Microsoft.Extensions.Options;
using OAuthProxy.AspNetCore.Abstractions;
using System.Net.Http.Headers;

namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class DummyHttpMessageHandler : DelegatingHandler
    {

        protected override async Task<HttpResponseMessage> SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
        {
            request.Headers.Add("X-Api-Key", "TestKey");

            return await base.SendAsync(request, cancellationToken);

        }
    }
}
