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
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        [JsonPropertyName("redirectUrl")]
        public string RedirectUrl { get; set; }

        public Dictionary<string, string> ExtraParameters { get; set; } = [];
    }
}
