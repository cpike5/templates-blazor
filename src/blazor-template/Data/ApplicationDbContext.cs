using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<InviteCode> InviteCodes { get; set; }
        public DbSet<EmailInvite> EmailInvites { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
        public DbSet<SystemHealthMetric> SystemHealthMetrics { get; set; }
        public DbSet<SettingsAuditLog> SettingsAuditLogs { get; set; }

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

            // Configure RolePermission entity
            builder.Entity<RolePermission>(entity =>
            {
                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => e.PermissionName);
                entity.HasIndex(e => new { e.RoleId, e.PermissionName });
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ApplicationSetting entity
            builder.Entity<ApplicationSetting>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.LastModified);
                entity.HasIndex(e => new { e.Category, e.Key });
                
                entity.HasOne(e => e.ModifiedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ModifiedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SystemHealthMetric entity
            builder.Entity<SystemHealthMetric>(entity =>
            {
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.MetricType);
                entity.HasIndex(e => new { e.MetricType, e.Timestamp });
            });

            // Configure SettingsAuditLog entity
            builder.Entity<SettingsAuditLog>(entity =>
            {
                entity.HasIndex(e => e.SettingsKey);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.SettingsKey, e.Timestamp });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}
