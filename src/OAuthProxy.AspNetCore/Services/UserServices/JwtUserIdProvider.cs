using Microsoft.AspNetCore.Http;
using OAuthProxy.AspNetCore.Abstractions;
using System.Security.Claims;

namespace OAuthProxy.AspNetCore.Services.UserServices
{
    public class JwtUserIdProvider : IUserIdProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtUserIdProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || (!user.Identity?.IsAuthenticated ?? false))
            {
                return null; // User is not authenticated
            }

            // Return the user ID from the JWT claims
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                return userIdClaim.Value; // Return the user ID from the JWT claim
            }
            
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return null;
        }
    }
}
