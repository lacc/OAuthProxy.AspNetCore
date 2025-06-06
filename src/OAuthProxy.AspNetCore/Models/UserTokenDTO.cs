using System.ComponentModel.DataAnnotations;

namespace OAuthProxy.AspNetCore.Models
{
    public class UserTokenDTO
    {
        [Key]
        public int Id { get; set; }
        public required string UserId { get; set; } // From Entra ID (e.g., Object ID)
        public required string ServiceName { get; set; } // e.g., "ServiceA", "ServiceB"
        
        [Required]
        public required string AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
