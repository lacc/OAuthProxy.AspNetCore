using System.ComponentModel.DataAnnotations;

namespace OAuthProxy.AspNetCore.Data
{
    internal class StateEntity
    {
        [Key]
        public required string StateId { get; set; }
        public required string UserId { get; set; }
        public required string ThirdPartyServiceProvider { get; set; }
        public DateTime ExpiresAt { get; set; }
        public required string StateSecret { get; set; }
        
    }
}
