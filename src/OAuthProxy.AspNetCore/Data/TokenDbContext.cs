using Microsoft.EntityFrameworkCore;

namespace OAuthProxy.AspNetCore.Data
{
    public class TokenDbContext(DbContextOptions<TokenDbContext> options) : DbContext(options)
    {
        public DbSet<ThirdPartyTokenEntity> OAuthTokens { get; set; }
        internal DbSet<StateEntity> OAuthStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ThirdPartyTokenEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.ThirdPartyServiceProvider }).IsUnique();
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.ThirdPartyServiceProvider).HasMaxLength(100);
            });

            modelBuilder.Entity<StateEntity>(entity =>
            {
                entity.HasIndex(e => new { e.StateId, e.ThirdPartyServiceProvider }).IsUnique();
                entity.Property(e => e.StateId).HasMaxLength(450);
                entity.Property(e => e.ThirdPartyServiceProvider).HasMaxLength(100);
            });
        }
    }
}
