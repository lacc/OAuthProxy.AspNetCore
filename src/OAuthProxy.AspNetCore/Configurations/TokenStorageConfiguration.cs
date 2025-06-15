using Microsoft.EntityFrameworkCore;

namespace OAuthProxy.AspNetCore.Configurations
{
    public class TokenStorageConfiguration
    {
        public Action<DbContextOptionsBuilder>? DatabaseOptions { get; set; }
        public bool AutoMigration { get; set; } = false;
    }
}
