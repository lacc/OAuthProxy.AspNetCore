using System.ComponentModel.DataAnnotations;

namespace OAuthProxy.AspNetCore.Data
{
    public class ThirdPartyTokenEntity
    {
        [Key]
        public int Id { get; set; }
        public required string UserId { get; set; } // From Entra ID (e.g., Object ID)
        public required string ThirdPartyServiceProvider { get; set; } // e.g., "ServiceA", "ServiceB"
        
        [Required]
        public required string AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
