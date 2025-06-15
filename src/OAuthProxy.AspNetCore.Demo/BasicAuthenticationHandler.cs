using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

internal class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get the authorization header from the request.
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Authorization header is missing.");
        }

        // Extract the username and password from the authorization header.
        string authorizationHeader = Request.Headers.Authorization!;
        string encodedUsernamePassword = authorizationHeader.Substring("Basic ".Length).Trim();
        byte[] decodedBytes = Convert.FromBase64String(encodedUsernamePassword);
        string[] usernamePassword = Encoding.UTF8.GetString(decodedBytes).Split(':');
        string username = usernamePassword[0];
        string password = usernamePassword[1];

        // Perform any custom authentication logic here.
        // In this example, any username and password combination is accepted.
        if (username == null || password == null)
        {
            return AuthenticateResult.Fail("Invalid username or password.");
        }

        var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, username),
        new Claim(ClaimTypes.Name, username),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return await Task.FromResult(AuthenticateResult.Success(ticket));
    }
}