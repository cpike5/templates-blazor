# Database Architecture Documentation

## Overview

The database architecture extends ASP.NET Core Identity with additional entities for invite systems, JWT token management, user activity tracking, file management, and theme preferences. Built on Entity Framework Core with SQL Server, featuring comprehensive indexing and relationship configurations.

## Core Entities

### ApplicationUser (`Data/ApplicationUser.cs`)

Extends `IdentityUser` to include custom application-specific properties.

```csharp
public class ApplicationUser : IdentityUser
{
    public string? ThemePreference { get; set; }  // Format: "theme|isDarkMode"
}
```

**Features:**
- Inherits all ASP.NET Identity properties (Email, PasswordHash, etc.)
- Custom theme preference storage
- Navigation properties to related entities

### UserActivity (`Data/UserActivity.cs`)

Tracks user actions and system events for auditing and analytics.

```csharp
public class UserActivity
{
    public int Id { get; set; }
    public string UserId { get; set; }           // Foreign key to ApplicationUser
    public string ActivityType { get; set; }    // Login, Logout, PasswordChange, etc.
    public string? Description { get; set; }    // Additional activity details
    public DateTime Timestamp { get; set; }     // When the activity occurred
    public string? IpAddress { get; set; }      // Client IP address
    public string? UserAgent { get; set; }      // Client user agent string
    
    // Navigation property
    public ApplicationUser User { get; set; }
}
```

**Use Cases:**
- Security auditing and monitoring
- User behavior analytics
- Compliance and logging requirements
- Debugging authentication issues

### InviteCode (`Data/InviteCode.cs`)

Manages short-form invitation codes for user registration.

```csharp
public class InviteCode
{
    public int Id { get; set; }
    public string Code { get; set; }             // 8-character alphanumeric code
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string CreatedByUserId { get; set; }  // Admin who created the code
    public string? UsedByUserId { get; set; }    // User who used the code
    public string? Notes { get; set; }           // Optional admin notes
    
    // Navigation properties
    public ApplicationUser CreatedByUser { get; set; }
    public ApplicationUser? UsedByUser { get; set; }
    
    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;
}
```

### EmailInvite (`Data/EmailInvite.cs`)

Manages email-based invitations with secure tokens.

```csharp
public class EmailInvite
{
    public int Id { get; set; }
    public string Email { get; set; }            // Target email address
    public string InviteToken { get; set; }      // 64-character URL-safe token
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string CreatedByUserId { get; set; }  // Admin who created the invite
    public string? UsedByUserId { get; set; }    // User who used the invite
    public string? Notes { get; set; }
    
    // Navigation properties
    public ApplicationUser CreatedByUser { get; set; }
    public ApplicationUser? UsedByUser { get; set; }
}
```

### RefreshToken (`Data/RefreshToken.cs`)

Stores JWT refresh tokens for API authentication.

```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }            // Base64-encoded random token
    public string UserId { get; set; }           // Foreign key to ApplicationUser
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; } // Token chain tracking
    
    // Navigation property
    public ApplicationUser User { get; set; }
    
    // Computed property
    public bool IsActive => !IsRevoked && ExpiryDate > DateTime.UtcNow;
}
```

### MediaFile (`Data/MediaFile.cs`)

Stores metadata and storage information for uploaded files.

```csharp
public class MediaFile
{
    public int Id { get; set; }
    public string FileName { get; set; }          // Original filename
    public string StoredFileName { get; set; }    // Internal UUID-based name
    public string ContentType { get; set; }       // MIME type
    public long FileSize { get; set; }            // Size in bytes
    public string FileHash { get; set; }          // SHA-256 hash
    public string StorageProvider { get; set; }   // "Local", "Azure", "S3"
    public string StoragePath { get; set; }       // Relative path
    public string? StorageContainer { get; set; } // Container/bucket name
    
    // Metadata
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? TagsJson { get; set; }         // JSON array of tags
    
    // Classification
    public MediaFileCategory Category { get; set; }
    public MediaFileVisibility Visibility { get; set; }
    public MediaProcessingStatus ProcessingStatus { get; set; }
    
    // Media Properties
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool HasThumbnail { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? ThumbnailSizes { get; set; }
    
    // Access & Tracking
    public string UploadedByUserId { get; set; }
    public string? SharedWithRoles { get; set; }   // JSON array of roles
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
    
    // Navigation property
    public ApplicationUser UploadedBy { get; set; }
}
```

**Features:**
- Complete file metadata storage
- SHA-256 hashing for deduplication
- Role-based access control through JSON arrays
- Media-specific properties (dimensions, duration)
- Thumbnail management and processing status
- Access tracking and audit trail

### MediaFileAccess (`Data/MediaFileAccess.cs`)

Tracks granular file access permissions and user access history.

```csharp
public class MediaFileAccess
{
    public int Id { get; set; }
    public int MediaFileId { get; set; }
    public string UserId { get; set; }
    public string? RoleName { get; set; }
    public MediaAccessLevel AccessLevel { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public MediaFile MediaFile { get; set; }
    public ApplicationUser User { get; set; }
    public ApplicationUser GrantedByUser { get; set; }
}
```

**Use Cases:**
- Granular permission management
- Time-limited access grants
- Access audit trails
- Role-based file sharing

## ApplicationDbContext (`Data/ApplicationDbContext.cs`)

The main Entity Framework context extending `IdentityDbContext`.

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<InviteCode> InviteCodes { get; set; }
    public DbSet<EmailInvite> EmailInvites { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }
    public DbSet<MediaFileAccess> MediaFileAccess { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Entity configurations...
    }
}
```

## Entity Configurations

### UserActivity Configuration

```csharp
builder.Entity<UserActivity>(entity =>
{
    // Indexes for query performance
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.Timestamp);
    entity.HasIndex(e => new { e.UserId, e.Timestamp });
    
    // Foreign key relationship
    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

**Indexing Strategy:**
- `UserId`: Fast lookup of activities for specific users
- `Timestamp`: Chronological queries and sorting
- `(UserId, Timestamp)`: Combined queries for user activity history

### InviteCode Configuration

```csharp
builder.Entity<InviteCode>(entity =>
{
    // Unique constraint on invitation codes
    entity.HasIndex(e => e.Code).IsUnique();
    entity.HasIndex(e => e.ExpiresAt);
    entity.HasIndex(e => e.CreatedAt);
    entity.HasIndex(e => new { e.IsUsed, e.ExpiresAt });
    
    // Relationships with proper cascade behavior
    entity.HasOne(e => e.CreatedByUser)
        .WithMany()
        .HasForeignKey(e => e.CreatedByUserId)
        .OnDelete(DeleteBehavior.Restrict);  // Prevent deleting admin with active codes
        
    entity.HasOne(e => e.UsedByUser)
        .WithMany()
        .HasForeignKey(e => e.UsedByUserId)
        .OnDelete(DeleteBehavior.SetNull);   // Allow user deletion
});
```

**Indexing Strategy:**
- `Code`: Unique index for fast code lookup and validation
- `ExpiresAt`: Efficient cleanup queries for expired codes
- `(IsUsed, ExpiresAt)`: Active code queries combining status and expiration

### EmailInvite Configuration

```csharp
builder.Entity<EmailInvite>(entity =>
{
    // Unique constraint on invitation tokens
    entity.HasIndex(e => e.InviteToken).IsUnique();
    entity.HasIndex(e => e.Email);
    entity.HasIndex(e => e.ExpiresAt);
    entity.HasIndex(e => e.CreatedAt);
    entity.HasIndex(e => new { e.IsUsed, e.ExpiresAt });
    
    // Relationship configuration
    entity.HasOne(e => e.CreatedByUser)
        .WithMany()
        .HasForeignKey(e => e.CreatedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
        
    entity.HasOne(e => e.UsedByUser)
        .WithMany()
        .HasForeignKey(e => e.UsedByUserId)
        .OnDelete(DeleteBehavior.SetNull);
});
```

### RefreshToken Configuration

```csharp
builder.Entity<RefreshToken>(entity =>
{
    // Unique constraint on tokens
    entity.HasIndex(e => e.Token).IsUnique();
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.ExpiryDate);
    entity.HasIndex(e => new { e.UserId, e.IsRevoked });
    
    // Cascade delete when user is deleted
    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### MediaFile Configuration

```csharp
builder.Entity<MediaFile>(entity =>
{
    // Indexes for query performance
    entity.HasIndex(e => e.UploadedByUserId);
    entity.HasIndex(e => e.Category);
    entity.HasIndex(e => e.Visibility);
    entity.HasIndex(e => e.ProcessingStatus);
    entity.HasIndex(e => e.ContentType);
    entity.HasIndex(e => e.FileHash);
    entity.HasIndex(e => e.CreatedAt);
    entity.HasIndex(e => new { e.UploadedByUserId, e.CreatedAt });
    entity.HasIndex(e => new { e.Visibility, e.Category });
    
    // String length constraints
    entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
    entity.Property(e => e.StoredFileName).HasMaxLength(255).IsRequired();
    entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
    entity.Property(e => e.FileHash).HasMaxLength(64).IsRequired();
    entity.Property(e => e.StorageProvider).HasMaxLength(50).IsRequired();
    entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
    entity.Property(e => e.StorageContainer).HasMaxLength(100);
    entity.Property(e => e.Title).HasMaxLength(200);
    entity.Property(e => e.Description).HasMaxLength(1000);
    entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
    
    // Foreign key relationship
    entity.HasOne(e => e.UploadedBy)
        .WithMany()
        .HasForeignKey(e => e.UploadedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

### MediaFileAccess Configuration

```csharp
builder.Entity<MediaFileAccess>(entity =>
{
    // Indexes for access control queries
    entity.HasIndex(e => e.MediaFileId);
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.RoleName);
    entity.HasIndex(e => e.ExpiresAt);
    entity.HasIndex(e => new { e.MediaFileId, e.UserId });
    entity.HasIndex(e => new { e.MediaFileId, e.RoleName });
    entity.HasIndex(e => new { e.UserId, e.AccessLevel });
    
    // Foreign key relationships
    entity.HasOne(e => e.MediaFile)
        .WithMany()
        .HasForeignKey(e => e.MediaFileId)
        .OnDelete(DeleteBehavior.Cascade);
        
    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        
    entity.HasOne(e => e.GrantedByUser)
        .WithMany()
        .HasForeignKey(e => e.GrantedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

## Database Schema Overview

### Core Identity Tables
- `AspNetUsers` - User accounts (extended with ThemePreference)
- `AspNetRoles` - Application roles
- `AspNetUserRoles` - User-role assignments
- `AspNetUserClaims` - User claims
- `AspNetRoleClaims` - Role-based claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - User tokens

### Custom Application Tables
- `UserActivities` - User activity and audit logging
- `InviteCodes` - Short-form invitation codes
- `EmailInvites` - Email-based invitations
- `RefreshTokens` - JWT refresh token storage
- `MediaFiles` - File metadata and storage information
- `MediaFileAccess` - File access permissions and audit trail

## Migration Strategy

### Initial Migration
The first migration creates the complete Identity schema plus custom entities:

```bash
# Generate initial migration
dotnet ef migrations add CreateIdentitySchema

# Apply to database
dotnet ef database update
```

### Subsequent Migrations
Each new feature adds migrations incrementally:

```bash
# User Activity tracking
dotnet ef migrations add AddUserActivity

# Invite system
dotnet ef migrations add AddInviteSystem  

# JWT refresh tokens
dotnet ef migrations add AddRefreshTokens

# Theme preferences
dotnet ef migrations add AddThemePreference
```

## Query Patterns

### User Activity Queries
```csharp
// Recent activity for a user
var recentActivity = await context.UserActivities
    .Where(a => a.UserId == userId)
    .OrderByDescending(a => a.Timestamp)
    .Take(50)
    .ToListAsync();

// Activity by type and date range
var loginActivity = await context.UserActivities
    .Where(a => a.ActivityType == "Login" && 
                a.Timestamp >= startDate && 
                a.Timestamp <= endDate)
    .ToListAsync();
```

### Invite System Queries
```csharp
// Active invite codes for admin
var activeCodes = await context.InviteCodes
    .Where(ic => ic.CreatedByUserId == adminId && 
                 !ic.IsUsed && 
                 ic.ExpiresAt > DateTime.UtcNow)
    .OrderByDescending(ic => ic.CreatedAt)
    .ToListAsync();

// Validate invite code
var inviteCode = await context.InviteCodes
    .FirstOrDefaultAsync(ic => ic.Code == code && 
                              !ic.IsUsed && 
                              ic.ExpiresAt > DateTime.UtcNow);
```

### Token Management Queries
```csharp
// Find active refresh token
var refreshToken = await context.RefreshTokens
    .Include(rt => rt.User)
    .FirstOrDefaultAsync(rt => rt.Token == token && 
                              !rt.IsRevoked && 
                              rt.ExpiryDate > DateTime.UtcNow);

// Clean expired tokens
var expiredTokens = await context.RefreshTokens
    .Where(rt => rt.ExpiryDate <= DateTime.UtcNow || rt.IsRevoked)
    .ToListAsync();
```

### Media File Queries
```csharp
// User's files with pagination
var userFiles = await context.MediaFiles
    .Where(mf => mf.UploadedByUserId == userId && 
                 mf.ProcessingStatus != MediaProcessingStatus.Deleted)
    .OrderByDescending(mf => mf.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Public files by category
var publicImages = await context.MediaFiles
    .Where(mf => mf.Visibility == MediaFileVisibility.Public && 
                 mf.Category == MediaFileCategory.Image &&
                 mf.ProcessingStatus == MediaProcessingStatus.Complete)
    .OrderByDescending(mf => mf.CreatedAt)
    .ToListAsync();

// File access check
var hasAccess = await context.MediaFiles
    .AnyAsync(mf => mf.Id == fileId && 
                   (mf.Visibility == MediaFileVisibility.Public ||
                    mf.UploadedByUserId == userId ||
                    (mf.Visibility == MediaFileVisibility.Shared && 
                     mf.SharedWithRoles.Contains(userRole))));

// Files by hash (deduplication)
var existingFile = await context.MediaFiles
    .FirstOrDefaultAsync(mf => mf.FileHash == fileHash &&
                              mf.ProcessingStatus != MediaProcessingStatus.Deleted);

// Files needing thumbnail processing
var pendingThumbnails = await context.MediaFiles
    .Where(mf => mf.Category == MediaFileCategory.Image &&
                 mf.ProcessingStatus == MediaProcessingStatus.Pending &&
                 !mf.HasThumbnail)
    .Take(10)
    .ToListAsync();
```

### File Access Queries
```csharp
// User's file permissions
var userAccess = await context.MediaFileAccess
    .Where(mfa => mfa.UserId == userId &&
                  (mfa.ExpiresAt == null || mfa.ExpiresAt > DateTime.UtcNow))
    .Include(mfa => mfa.MediaFile)
    .ToListAsync();

// Files shared with role
var roleFiles = await context.MediaFiles
    .Where(mf => mf.Visibility == MediaFileVisibility.Shared)
    .Where(mf => mf.SharedRoles.Contains(roleName) ||
                 context.MediaFileAccess.Any(mfa => mfa.MediaFileId == mf.Id &&
                                                   mfa.RoleName == roleName &&
                                                   (mfa.ExpiresAt == null || mfa.ExpiresAt > DateTime.UtcNow)))
    .ToListAsync();
```

## Performance Considerations

### Indexing Best Practices
- **Unique Indexes**: Ensure data integrity and fast lookups
- **Composite Indexes**: Optimize multi-column queries
- **Covering Indexes**: Include frequently accessed columns
- **Selective Indexes**: Consider filtered indexes for large tables

### Query Optimization
- **Projection**: Select only needed columns
- **Pagination**: Implement proper paging for large result sets
- **Caching**: Cache frequently accessed configuration data
- **Connection Pooling**: Leverage EF Core connection pooling

### Maintenance Tasks
```csharp
// Regular cleanup of old data
public async Task CleanupOldDataAsync()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-90);
    
    // Clean old user activities
    var oldActivities = await context.UserActivities
        .Where(a => a.Timestamp < cutoffDate)
        .ToListAsync();
    
    context.UserActivities.RemoveRange(oldActivities);
    
    // Clean expired invites
    var expiredInvites = await context.InviteCodes
        .Where(ic => ic.ExpiresAt < DateTime.UtcNow && !ic.IsUsed)
        .ToListAsync();
    
    context.InviteCodes.RemoveRange(expiredInvites);
    
    // Clean soft-deleted media files older than retention period
    var deletedFiles = await context.MediaFiles
        .Where(mf => mf.ProcessingStatus == MediaProcessingStatus.Deleted &&
                     mf.LastModifiedAt < cutoffDate)
        .ToListAsync();
    
    context.MediaFiles.RemoveRange(deletedFiles);
    
    // Clean expired file access grants
    var expiredAccess = await context.MediaFileAccess
        .Where(mfa => mfa.ExpiresAt.HasValue && mfa.ExpiresAt < DateTime.UtcNow)
        .ToListAsync();
    
    context.MediaFileAccess.RemoveRange(expiredAccess);
    
    await context.SaveChangesAsync();
}
```

## Security Considerations

### Data Protection
- **Sensitive Data**: Never store passwords or tokens in plain text
- **Encryption**: Consider encryption for sensitive user data
- **Audit Trail**: Maintain comprehensive activity logs
- **Access Control**: Use proper foreign key constraints

### Database Security
- **Connection Strings**: Secure storage of connection strings
- **Permissions**: Minimal database permissions for application accounts
- **Backup Security**: Encrypt database backups
- **Network Security**: Secure database network access

## Backup and Recovery

### Backup Strategy
```sql
-- Full database backup
BACKUP DATABASE [blazor_template] 
TO DISK = 'C:\Backups\blazor_template_full.bak'
WITH FORMAT, INIT;

-- Transaction log backup
BACKUP LOG [blazor_template]
TO DISK = 'C:\Backups\blazor_template_log.trn';
```

### Disaster Recovery
- **Point-in-time recovery** using transaction log backups
- **High availability** with Always On Availability Groups
- **Geographic redundancy** for critical deployments
- **Regular recovery testing** and documentation

## Monitoring and Diagnostics

### Entity Framework Logging
```csharp
// Configure EF Core logging in Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Configure query logging
    options.LogTo(Console.WriteLine, LogLevel.Information);
});
```

### Performance Monitoring
- **Query execution time** monitoring
- **Connection pool** health monitoring
- **Database lock** and blocking detection
- **Index usage** analysis

## Troubleshooting

### Common Issues

#### Migration Failures
1. Check for conflicting schema changes
2. Verify database connectivity
3. Review migration dependencies
4. Ensure proper permissions

#### Performance Problems
1. Analyze query execution plans
2. Check for missing indexes
3. Review connection pool settings
4. Monitor database resource usage

#### Integrity Issues
1. Verify foreign key relationships
2. Check for orphaned records
3. Validate unique constraints
4. Review cascade delete behavior

### Development Tips
- Use **EF Core tools** for migration management
- **Seed data** for development and testing
- **Database snapshots** for reliable test databases
- **Connection string management** across environments