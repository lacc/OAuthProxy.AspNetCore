using Microsoft.AspNetCore.Authentication.BearerToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Abstractions
{
    public record struct AccessTokenBuilderResponse(string? AccessToken, string? ErrorMessage, HttpStatusCode StatusCode = HttpStatusCode.OK);
    public interface IAccessTokenBuilder
    {
        Task<AccessTokenBuilderResponse> BuildAccessTokenAsync(HttpRequestMessage request, string userId, string serviceName);
    }
}
