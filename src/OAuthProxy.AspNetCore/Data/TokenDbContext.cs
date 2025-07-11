using Microsoft.EntityFrameworkCore;

namespace OAuthProxy.AspNetCore.Data
{
    public class TokenDbContext(DbContextOptions<TokenDbContext> options) : DbContext(options)
    {
        public DbSet<ThirdPartyTokenEntity> OAuthTokens { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ThirdPartyTokenEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.ThirdPartyServiceProvider }).IsUnique();
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.ThirdPartyServiceProvider).HasMaxLength(100);
            });
        }
    }
}
