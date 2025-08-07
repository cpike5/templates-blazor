# Admin Features - Backend Requirements Analysis

## Overview

This document outlines the backend services and methods required to support the admin functionality in the Blazor Template application. The analysis is based on the existing admin components (`Components/Admin/`) and leverages ASP.NET Core Identity as the foundation.

## Admin Components Analyzed

- **Dashboard.razor** - Main admin dashboard with statistics and recent activity
- **Users.razor** - User listing and management interface  
- **UserDetails.razor** - Individual user profile and role management
- **Roles.razor** - Role listing and management interface
- **RoleForm.razor** - Role creation and editing form
- **Settings.razor** - System settings and configuration

## Required Backend Services

### 1. User Management Service

**Purpose:** Handle all user-related operations for admin interface

**Core Identity Dependencies:**
- `UserManager<IdentityUser>` - Built-in Identity service
- `SignInManager<IdentityUser>` - Built-in Identity service

**Required Methods:**
```csharp
// User CRUD operations
Task<PagedResult<UserDto>> GetUsersAsync(UserSearchCriteria criteria)
Task<UserDetailDto> GetUserByIdAsync(string userId)
Task<IdentityResult> CreateUserAsync(CreateUserRequest request)
Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request)
Task<IdentityResult> DeleteUserAsync(string userId)

// User account management
Task<IdentityResult> LockUserAsync(string userId)
Task<IdentityResult> UnlockUserAsync(string userId)
Task<IdentityResult> ResetUserPasswordAsync(string userId)
Task<IdentityResult> ResendEmailConfirmationAsync(string userId)

// User statistics
Task<UserStatsDto> GetUserStatisticsAsync()
Task<List<UserActivityDto>> GetUserActivityAsync(string userId)
```

### 2. Role Management Service

**Purpose:** Handle role operations and assignments

**Core Identity Dependencies:**
- `RoleManager<IdentityRole>` - Built-in Identity service

**Required Methods:**
```csharp
// Role CRUD operations
Task<List<RoleDto>> GetRolesAsync()
Task<RoleDetailDto> GetRoleByIdAsync(string roleId)
Task<IdentityResult> CreateRoleAsync(CreateRoleRequest request)
Task<IdentityResult> UpdateRoleAsync(string roleId, UpdateRoleRequest request)
Task<IdentityResult> DeleteRoleAsync(string roleId)

// Role assignments
Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
Task<List<string>> GetUserRolesAsync(string userId)
Task<List<UserDto>> GetUsersInRoleAsync(string roleName)

// Role statistics and metadata
Task<RoleStatsDto> GetRoleStatisticsAsync()
Task<bool> IsSystemRoleAsync(string roleName)
```

### 3. Dashboard Analytics Service

**Purpose:** Provide dashboard statistics and system health information

**Required Methods:**
```csharp
// System statistics
Task<DashboardStatsDto> GetDashboardStatisticsAsync()
Task<List<RecentActivityDto>> GetRecentActivityAsync(int count = 10)
Task<SystemHealthDto> GetSystemHealthAsync()

// Security monitoring
Task<List<LoginAttemptDto>> GetRecentLoginAttemptsAsync()
Task<List<SecurityAlertDto>> GetSecurityAlertsAsync()
Task<int> GetFailedLoginCountAsync(TimeSpan timespan)
```

### 4. Settings Management Service

**Purpose:** Handle system configuration and settings

**Required Methods:**
```csharp
// System configuration
Task<SystemSettingsDto> GetSystemSettingsAsync()
Task UpdateSystemSettingsAsync(UpdateSystemSettingsRequest request)
Task ResetSettingsToDefaultAsync()

// Email configuration
Task<List<EmailTemplateDto>> GetEmailTemplatesAsync()
Task UpdateEmailTemplateAsync(string templateId, EmailTemplateDto template)
Task<EmailSettingsDto> GetEmailSettingsAsync()
Task UpdateEmailSettingsAsync(EmailSettingsDto settings)

// Security settings
Task<SecuritySettingsDto> GetSecuritySettingsAsync()
Task UpdateSecuritySettingsAsync(SecuritySettingsDto settings)

// Backup management
Task<List<BackupDto>> GetBackupsAsync()
Task<BackupDto> CreateBackupAsync()
Task RestoreBackupAsync(string backupId)
```

### 5. Activity Logging Service

**Purpose:** Track and retrieve user and system activities

**Required Methods:**
```csharp
// Activity logging
Task LogUserActivityAsync(string userId, string action, string details)
Task LogSystemActivityAsync(string action, string details, string? userId = null)

// Activity retrieval
Task<List<ActivityLogDto>> GetUserActivityLogAsync(string userId, int pageSize = 10)
Task<List<ActivityLogDto>> GetSystemActivityLogAsync(int pageSize = 50)
Task<List<ActivityLogDto>> GetRecentActivityAsync(int count = 10)
```

## Data Transfer Objects (DTOs)

### Core DTOs

```csharp
public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public List<string> Roles { get; set; }
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class RoleDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int PendingUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TodaySessions { get; set; }
    public int SecurityAlerts { get; set; }
}

public class SystemSettingsDto
{
    public string SiteTitle { get; set; }
    public string AdminEmail { get; set; }
    public string TimeZone { get; set; }
    public string DateFormat { get; set; }
    public bool MaintenanceMode { get; set; }
    public bool UserRegistrationEnabled { get; set; }
    public bool GuestAccessEnabled { get; set; }
}

public class SecuritySettingsDto
{
    public int PasswordMinLength { get; set; }
    public int SessionTimeoutMinutes { get; set; }
    public bool RequireEmailConfirmation { get; set; }
    public bool RequireTwoFactor { get; set; }
    public bool AccountLockoutEnabled { get; set; }
    public bool PasswordComplexityRequired { get; set; }
}
```

## ASP.NET Core Identity Integration

### Existing Infrastructure
The project already includes:
- **ApplicationDbContext** extending `IdentityDbContext`
- **UserRoleService** (Services/UserRoleService.cs)
- **DataSeeder** for initial role setup
- **FirstTimeSetupService** for application initialization

### Leverage Built-in Identity Features
- **Password policies** via `IdentityOptions.Password`
- **Account lockout** via `IdentityOptions.Lockout`
- **Email confirmation** via `IdentityOptions.SignIn.RequireConfirmedEmail`
- **Two-factor authentication** via Identity's 2FA system
- **Role-based authorization** via `[Authorize(Roles = "")]` attributes

### Additional Services to Implement

1. **AdminUserService** 
   - Wraps `UserManager<IdentityUser>` with admin-specific functionality
   - Handles user statistics and bulk operations

2. **AdminRoleService**
   - Wraps `RoleManager<IdentityRole>` with role management features
   - Manages role assignments and permissions

3. **ActivityLogService**
   - Custom service for audit trails and activity logging
   - Stores activity data in custom database tables

4. **DashboardService**
   - Aggregates statistics from various sources
   - Monitors system health and performance

5. **SystemSettingsService**
   - Manages application configuration stored in database
   - Handles settings validation and updates

## Implementation Notes

### Database Considerations
- Identity tables are already created via migrations
- Additional tables needed for:
  - ActivityLogs
  - SystemSettings
  - EmailTemplates
  - Backups metadata

### Security Considerations
- All admin services should require `Administrator` role
- Sensitive operations should log activities
- Settings changes should be validated and logged
- User data access should be audited

### Performance Considerations
- Dashboard statistics should be cached
- Activity logs should be paginated
- User/role queries should support filtering and sorting
- Consider background services for statistics calculation

## Next Steps

1. Implement the core service classes
2. Create required database entities and migrations
3. Add API controllers for admin operations
4. Wire up the Blazor components with real data
5. Implement proper authorization and logging
6. Add unit tests for all services

## Related Files

- `Services/UserRoleService.cs` - Existing role service
- `Data/ApplicationDbContext.cs` - Database context
- `Data/DataSeeder.cs` - Initial data seeding
- `Components/Admin/` - Frontend components requiring these services