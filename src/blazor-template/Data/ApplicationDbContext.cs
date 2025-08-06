using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<InviteCode> InviteCodes { get; set; }
        public DbSet<EmailInvite> EmailInvites { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure UserActivity entity
            builder.Entity<UserActivity>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure InviteCode entity
            builder.Entity<InviteCode>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.IsUsed, e.ExpiresAt });
                
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.UsedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UsedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure EmailInvite entity
            builder.Entity<EmailInvite>(entity =>
            {
                entity.HasIndex(e => e.InviteToken).IsUnique();
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.IsUsed, e.ExpiresAt });
                
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.UsedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UsedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure RefreshToken entity
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => new { e.UserId, e.IsRevoked });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
