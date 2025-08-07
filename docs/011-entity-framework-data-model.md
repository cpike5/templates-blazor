# Entity Framework Data Model Documentation

## Overview

This document provides a comprehensive overview of the Entity Framework Core data model used in the Blazor Server application. The application uses ASP.NET Core Identity integrated with custom entities to provide user management, authentication, invitation systems, activity tracking, and JWT token management.

**Database Provider**: SQL Server  
**Entity Framework Version**: 8.0.18  
**Identity Integration**: ASP.NET Core Identity with IdentityDbContext

## Entity Relationship Diagram (Textual)

```
AspNetUsers (ApplicationUser)
    ├── UserActivities (1:N) - Cascade Delete
    ├── InviteCodes (Creator) (1:N) - Restrict Delete
    ├── InviteCodes (User) (1:N) - Set Null Delete
    ├── EmailInvites (Creator) (1:N) - Restrict Delete
    ├── EmailInvites (User) (1:N) - Set Null Delete
    └── RefreshTokens (1:N) - Cascade Delete

Identity Tables (Standard ASP.NET Core Identity)
    ├── AspNetRoles
    ├── AspNetUserRoles
    ├── AspNetUserClaims
    ├── AspNetUserLogins
    ├── AspNetUserTokens
    └── AspNetRoleClaims
```

## Entities

### ApplicationUser (Identity Extension)

**Table**: `AspNetUsers`  
**Purpose**: Extends the standard ASP.NET Core Identity user with custom properties

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | nvarchar(450) | Primary Key | User identifier (inherited from Identity) |
| UserName | nvarchar(256) | Unique, Indexed | Username (inherited from Identity) |
| Email | nvarchar(256) | Indexed | User email (inherited from Identity) |
| EmailConfirmed | bit | Not Null | Email confirmation status (inherited from Identity) |
| PasswordHash | nvarchar(max) | | Hashed password (inherited from Identity) |
| SecurityStamp | nvarchar(max) | | Security token (inherited from Identity) |
| ConcurrencyStamp | nvarchar(max) | | Concurrency control (inherited from Identity) |
| PhoneNumber | nvarchar(max) | | Phone number (inherited from Identity) |
| PhoneNumberConfirmed | bit | Not Null | Phone confirmation status (inherited from Identity) |
| TwoFactorEnabled | bit | Not Null | 2FA status (inherited from Identity) |
| LockoutEnd | datetimeoffset | | Lockout end time (inherited from Identity) |
| LockoutEnabled | bit | Not Null | Lockout capability (inherited from Identity) |
| AccessFailedCount | int | Not Null | Failed login attempts (inherited from Identity) |
| NormalizedUserName | nvarchar(256) | Indexed | Normalized username (inherited from Identity) |
| NormalizedEmail | nvarchar(256) | Indexed | Normalized email (inherited from Identity) |
| **ThemePreference** | nvarchar(max) | Nullable | **Custom**: User's theme selection |

**Custom Properties**:
- `ThemePreference`: Stores user's preferred UI theme (light/dark/auto)

**Relationships**:
- One-to-Many with UserActivities (Creator)
- One-to-Many with InviteCodes (Creator and User)
- One-to-Many with EmailInvites (Creator and User)
- One-to-Many with RefreshTokens

### UserActivity

**Table**: `UserActivities`  
**Purpose**: Tracks user actions and system events for auditing and monitoring

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | nvarchar(450) | Primary Key | GUID-based unique identifier |
| UserId | nvarchar(450) | Foreign Key, Not Null, Indexed | Reference to AspNetUsers |
| Action | nvarchar(200) | Not Null, Max Length 200 | Type of action performed |
| Details | nvarchar(1000) | Not Null, Max Length 1000 | Detailed description of the action |
| Timestamp | datetime2 | Not Null, Indexed | When the action occurred (UTC) |
| IpAddress | nvarchar(45) | Nullable, Max Length 45 | IP address of the user |
| UserAgent | nvarchar(500) | Nullable, Max Length 500 | Browser/client user agent |

**Indexes**:
- `IX_UserActivities_UserId`: Single column index for user lookup
- `IX_UserActivities_Timestamp`: Single column index for time-based queries
- `IX_UserActivities_UserId_Timestamp`: Composite index for user timeline queries

**Relationships**:
- Many-to-One with ApplicationUser (Cascade Delete)

**Business Logic**:
- Uses GUID string for primary key for better distribution
- Automatically sets UTC timestamp on creation
- Cascade delete ensures activities are removed when user is deleted

### InviteCode

**Table**: `InviteCodes`  
**Purpose**: Manages shareable invitation codes for user registration

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | Primary Key, Identity | Auto-incrementing identifier |
| Code | nvarchar(50) | Not Null, Unique, Max Length 50 | The invitation code |
| CreatedAt | datetime2 | Not Null | When the code was created |
| ExpiresAt | datetime2 | Not Null, Indexed | When the code expires |
| IsUsed | bit | Not Null, Default False | Whether code has been used |
| UsedAt | datetime2 | Nullable | When the code was used |
| UsedByUserId | nvarchar(450) | Foreign Key, Nullable | User who used the code |
| CreatedByUserId | nvarchar(450) | Foreign Key, Not Null | User who created the code |
| Notes | nvarchar(500) | Nullable, Max Length 500 | Optional notes about the code |

**Indexes**:
- `IX_InviteCodes_Code`: Unique index on code for fast lookup
- `IX_InviteCodes_ExpiresAt`: Index for expiration-based queries
- `IX_InviteCodes_CreatedAt`: Index for creation time queries
- `IX_InviteCodes_IsUsed_ExpiresAt`: Composite index for finding valid codes

**Relationships**:
- Many-to-One with ApplicationUser (CreatedByUser) - Restrict Delete
- Many-to-One with ApplicationUser (UsedByUser) - Set Null Delete

**Business Logic**:
- Computed properties: `IsExpired`, `IsValid`
- Restrict delete on creator prevents orphaned codes
- Set null delete on user allows code history to persist

### EmailInvite

**Table**: `EmailInvites`  
**Purpose**: Manages email-based invitations with tokens for registration

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | Primary Key, Identity | Auto-incrementing identifier |
| Email | nvarchar(255) | Not Null, Indexed, Max Length 255, Email Format | Target email address |
| InviteToken | nvarchar(500) | Not Null, Unique, Max Length 500 | Unique invitation token |
| CreatedAt | datetime2 | Not Null, Indexed | When the invite was created |
| ExpiresAt | datetime2 | Not Null, Indexed | When the invite expires |
| IsUsed | bit | Not Null, Default False | Whether invite has been used |
| UsedAt | datetime2 | Nullable | When the invite was used |
| UsedByUserId | nvarchar(450) | Foreign Key, Nullable | User who used the invite |
| CreatedByUserId | nvarchar(450) | Foreign Key, Not Null | User who created the invite |
| Notes | nvarchar(500) | Nullable, Max Length 500 | Optional notes about the invite |
| EmailSentAt | datetime2 | Nullable | When the email was sent |

**Indexes**:
- `IX_EmailInvites_InviteToken`: Unique index on token for security
- `IX_EmailInvites_Email`: Index for email lookup
- `IX_EmailInvites_ExpiresAt`: Index for expiration queries
- `IX_EmailInvites_CreatedAt`: Index for creation time queries
- `IX_EmailInvites_IsUsed_ExpiresAt`: Composite index for finding valid invites

**Relationships**:
- Many-to-One with ApplicationUser (CreatedByUser) - Restrict Delete
- Many-to-One with ApplicationUser (UsedByUser) - Set Null Delete

**Business Logic**:
- Email address validation through data annotation
- Computed properties: `IsExpired`, `IsValid`
- Tracks email delivery status
- Same deletion behavior as InviteCodes

### RefreshToken

**Table**: `RefreshTokens`  
**Purpose**: Manages JWT refresh tokens for API authentication

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | Primary Key | GUID identifier |
| Token | nvarchar(450) | Not Null, Unique, Indexed | The refresh token |
| UserId | nvarchar(450) | Foreign Key, Not Null, Indexed | Reference to AspNetUsers |
| ExpiryDate | datetime2 | Not Null, Indexed | When the token expires |
| IsRevoked | bit | Not Null, Default False | Whether token has been revoked |
| CreatedAt | datetime2 | Not Null | When the token was created |
| RevokedAt | datetime2 | Nullable | When the token was revoked |
| RevokedByIp | nvarchar(max) | Nullable | IP address that revoked the token |
| ReplacedByToken | nvarchar(max) | Nullable | Token that replaced this one |

**Indexes**:
- `IX_RefreshTokens_Token`: Unique index on token for fast lookup
- `IX_RefreshTokens_UserId`: Index for user token queries
- `IX_RefreshTokens_ExpiryDate`: Index for expiration cleanup
- `IX_RefreshTokens_UserId_IsRevoked`: Composite index for active user tokens

**Relationships**:
- Many-to-One with ApplicationUser (Cascade Delete)

**Business Logic**:
- Uses GUID for better security
- Computed properties: `IsActive`, `IsExpired`
- Supports token rotation via `ReplacedByToken`
- Tracks revocation metadata for security auditing

## ASP.NET Core Identity Tables

The application uses the standard ASP.NET Core Identity tables with default configurations:

### AspNetRoles
- Standard role management
- Contains Administrator, User, Guest roles (configured via seeding)

### AspNetUserRoles
- Many-to-Many relationship between users and roles
- Managed automatically by Identity system

### AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
- Standard Identity tables for claims-based authentication
- External login providers (Google OAuth configured)
- Token management for password resets, email confirmation

## Migration History

### Initial Migration (CreateIdentitySchema)
- Creates all standard ASP.NET Core Identity tables
- Sets up basic user authentication infrastructure

### 20250805203944_AddUserActivity
- **Added**: UserActivities table
- **Indexes**: UserId, Timestamp, and composite (UserId, Timestamp)
- **Relationships**: Foreign key to AspNetUsers with Cascade delete

### 20250806010650_AddInviteSystem
- **Added**: InviteCodes table with unique code constraint
- **Added**: EmailInvites table with unique token constraint
- **Indexes**: Multiple indexes for performance on codes, emails, expiration dates
- **Relationships**: Foreign keys to AspNetUsers with Restrict/SetNull delete behaviors

### 20250806014251_AddRefreshTokens
- **Added**: RefreshTokens table
- **Indexes**: Unique token index, user and expiration indexes
- **Relationships**: Foreign key to AspNetUsers with Cascade delete

### 20250806134053_AddThemePreference
- **Modified**: AspNetUsers table
- **Added**: ThemePreference column for user UI preferences

## Database Seeding and Initialization

### DataSeeder Class
The `DataSeeder` class handles initial database population and is executed during application startup when setup mode is enabled.

**Seeding Process**:

1. **Role Creation**: 
   - Creates roles defined in `ConfigurationOptions.Administration.UserRoles`
   - Default roles: Administrator, User, Guest

2. **Admin User Assignment**:
   - Finds user with email matching `ConfigurationOptions.Administration.AdminEmail`
   - Assigns Administrator role if not already assigned
   - Logs warning if admin user doesn't exist

3. **Guest User Creation** (if enabled):
   - Creates demo user with email from configuration
   - Generates secure random password (16 characters, mixed case, digits, symbols)
   - Sets account as email confirmed with lockout disabled
   - Creates "Guest" role and assigns it to the user
   - Logs credentials prominently for demo access

**Configuration Dependencies**:
- `Site:Administration:AdminEmail`: Email for automatic admin privileges
- `Site:Administration:UserRoles`: List of roles to create
- `Site:Setup:EnableGuestUser`: Controls guest account creation
- `Site:Setup:GuestUser:Email`: Guest account email (default: guest@test.com)

**Security Considerations**:
- Guest passwords are cryptographically secure (not hard-coded)
- Admin assignment only works for existing users
- Role creation is idempotent
- All operations are logged for audit trails

## Performance Considerations

### Indexing Strategy

**Single Column Indexes**:
- All foreign keys are indexed for join performance
- Timestamp columns indexed for time-based queries
- Unique constraints on codes and tokens for security

**Composite Indexes**:
- `UserId + Timestamp`: Optimizes user activity timeline queries
- `IsUsed + ExpiresAt`: Optimizes finding valid invites/codes
- `UserId + IsRevoked`: Optimizes active token lookup

### Query Optimization

**UserActivity**:
- Composite index supports efficient user activity history queries
- Cascade delete reduces need for manual cleanup

**Invite Systems**:
- Unique indexes on codes and tokens prevent duplicates
- Composite indexes support efficient validity checks
- Separate creator/user foreign keys allow proper audit trails

**RefreshTokens**:
- Unique token index ensures fast authentication
- Composite userId + isRevoked index optimizes active token queries
- GUID primary key provides better distribution

## Security Features

### Data Protection
- All sensitive tokens use unique constraints to prevent duplicates
- Foreign key constraints maintain referential integrity
- Computed properties prevent direct state manipulation

### Audit Trails
- UserActivity tracks all user actions with IP and user agent
- Invite systems track creator and usage information
- RefreshTokens track revocation metadata

### Deletion Policies
- **Cascade**: User deletion removes activities and tokens (cleanup)
- **Restrict**: Prevents deletion of users who created invites (audit)
- **SetNull**: Preserves invite history when users are deleted

## Configuration Integration

The data model integrates with the application's configuration system through:

- **ConfigurationOptions**: Defines admin roles and emails
- **Setup Mode**: Controls automatic seeding behavior
- **Guest User System**: Manages demo account creation
- **Role Management**: Configures available user roles

This integration ensures the database schema supports the application's security model and operational requirements while maintaining flexibility for different deployment scenarios.