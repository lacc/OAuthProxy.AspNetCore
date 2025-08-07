using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Data
{
    public class ClientCredentialsConfigEntity
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }

        [Key]
        public int Id { get; set; }
        public required string UserId { get; set; } // From Entra ID (e.g., Object ID)
        public required string ThirdPartyServiceProvider { get; set; } // e.g., "ServiceA", "ServiceB"
            
        public string AccessToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
