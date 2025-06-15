using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Handlers
{
    internal class OAuthProxyMiddleware
    {
        private readonly RequestDelegate _next;

        public OAuthProxyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IProxyRequestContext proxyRequestContext)
        {
            if (context.Request.Path.Value.StartsWith(OAuthProxyApiMapper.ProxyAipPrefix))
            {
                var serviceName = context.Request.Path.Value
                    .Remove(0, OAuthProxyApiMapper.ProxyAipPrefix.Length)
                    .Trim('/')
                    .Split("/")[0];

                context.RequestServices.GetRequiredService<IProxyRequestContext>()
                    .SetServiceName(serviceName);

                proxyRequestContext.SetServiceName(serviceName);
            }
            await _next(context);
        }
    }
}
