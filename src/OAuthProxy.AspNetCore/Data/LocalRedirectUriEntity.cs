using System.ComponentModel.DataAnnotations;

namespace OAuthProxy.AspNetCore.Data
{
    internal class LocalRedirectUriEntity
    {
        public required string LocalRedirectUrl { get; set; }
        [Key]
        public required string AuthState { get; set; }
        public DateTime CreatedAt { get; internal set; }
    }
}
