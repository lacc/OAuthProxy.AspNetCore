using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Services.StateManagement
{
    public class AuthorizationStateParameters
    {
        public string? UserId { get; set; }
        public string? RedirectUrl { get; set; }

        public Dictionary<string, string> ExtraParameters { get; set; } = [];
    }
}
