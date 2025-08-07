# Database Architecture Documentation

## Overview

The database architecture extends ASP.NET Core Identity with additional entities for invite systems, JWT token management, user activity tracking, and theme preferences. Built on Entity Framework Core with SQL Server, featuring comprehensive indexing and relationship configurations.

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

## ApplicationDbContext (`Data/ApplicationDbContext.cs`)

The main Entity Framework context extending `IdentityDbContext`.

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<InviteCode> InviteCodes { get; set; }
    public DbSet<EmailInvite> EmailInvites { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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