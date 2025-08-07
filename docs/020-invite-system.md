# Invite System Documentation

## Overview

The Invite System provides secure, controlled user registration through two mechanisms:
- **Invite Codes**: Short alphanumeric codes for easy sharing
- **Email Invites**: Token-based invitations sent via email

The system supports invitation-only registration, preventing unauthorized signups while maintaining flexibility for different use cases.

## Core Components

### InviteService (`Services/InviteService.cs`)

Central service managing both invitation types with comprehensive validation and lifecycle management.

**Key Features:**
- Secure code/token generation using cryptographic randomness
- Configurable expiration times
- Usage tracking and validation
- Admin quotas and rate limiting
- Automatic cleanup of expired invites

### Data Models

#### InviteCode (`Data/InviteCode.cs`)
```csharp
public class InviteCode
{
    public int Id { get; set; }
    public string Code { get; set; }           // 8-character alphanumeric
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string CreatedByUserId { get; set; }
    public string? UsedByUserId { get; set; }
    public string? Notes { get; set; }
    
    // Computed Properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;
}
```

#### EmailInvite (`Data/EmailInvite.cs`)
```csharp
public class EmailInvite
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string InviteToken { get; set; }    // 64-character URL-safe token
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string CreatedByUserId { get; set; }
    public string? UsedByUserId { get; set; }
    public string? Notes { get; set; }
}
```

## Configuration

### Invite System Settings (`appsettings.json`)

```json
{
  "Site": {
    "Setup": {
      "InviteOnly": {
        "EnableInviteOnly": true,                     // Require invites for registration
        "EnableEmailInvites": false,                  // Enable email invitation system
        "EnableInviteCodes": true,                    // Enable invitation code system
        "DefaultCodeExpirationHours": 24,             // Default expiration for codes
        "DefaultEmailInviteExpirationHours": 72,      // Default expiration for emails
        "MaxActiveCodesPerAdmin": 50                  // Maximum active codes per admin
      }
    }
  }
}
```

**Configuration Options:**
- `EnableInviteOnly`: When true, prevents registration without valid invites
- `EnableEmailInvites`: Enables email-based invitation system
- `EnableInviteCodes`: Enables short code invitation system
- `DefaultCodeExpirationHours`: Default expiration time for new invite codes
- `DefaultEmailInviteExpirationHours`: Default expiration time for email invites
- `MaxActiveCodesPerAdmin`: Limits active invite codes per administrator

## Service Methods

### InviteService Interface
```csharp
public interface IInviteService
{
    Task<InviteCode> GenerateInviteCodeAsync(string createdByUserId, string? notes = null, int? expirationHours = null);
    Task<EmailInvite> GenerateEmailInviteAsync(string email, string createdByUserId, string? notes = null, int? expirationHours = null);
    Task<InviteCode?> ValidateInviteCodeAsync(string code);
    Task<EmailInvite?> ValidateEmailInviteAsync(string token);
    Task<bool> UseInviteCodeAsync(string code, string usedByUserId);
    Task<bool> UseEmailInviteAsync(string token, string usedByUserId);
    Task<IEnumerable<InviteCode>> GetActiveInviteCodesAsync(string createdByUserId);
    Task<IEnumerable<EmailInvite>> GetActiveEmailInvitesAsync(string createdByUserId);
    Task<bool> CanCreateMoreCodesAsync(string userId);
    Task CleanupExpiredInvitesAsync();
}
```

### Key Operations

#### Generating Invites
```csharp
// Generate invite code
var inviteCode = await _inviteService.GenerateInviteCodeAsync(adminUserId, "For new team member");

// Generate email invite
var emailInvite = await _inviteService.GenerateEmailInviteAsync(
    "newuser@company.com", 
    adminUserId, 
    "Welcome to the team"
);
```

#### Validating Invites
```csharp
// Validate invite code during registration
var inviteCode = await _inviteService.ValidateInviteCodeAsync("ABC12345");
if (inviteCode == null)
{
    // Invalid or expired code
}

// Use invite after successful registration
await _inviteService.UseInviteCodeAsync("ABC12345", newUserId);
```

## Admin Interface

### Invite Management Page (`Components/Admin/Invites.razor`)

Provides administrators with:
- **Code Generation**: Create new invite codes with custom expiration
- **Email Invites**: Generate email-based invitations (when enabled)
- **Active Invites**: View all active invitations
- **Usage History**: Track invitation usage and statistics
- **Bulk Operations**: Manage multiple invitations efficiently

**Features:**
- Real-time validation of admin quotas
- Copy-to-clipboard functionality for codes
- Visual expiration indicators
- Usage statistics and reporting

## Security Features

### Code Generation
- **Cryptographic Randomness**: Uses `RandomNumberGenerator` for secure code generation
- **Character Set**: Excludes ambiguous characters (0, O, I, l) for readability
- **Length**: 8-character codes provide sufficient entropy while remaining manageable
- **Case Consistency**: Uppercase codes for visual clarity

### Token Generation
- **Length**: 64-character URL-safe tokens for email invites
- **Encoding**: Base64URL encoding without padding characters
- **Collision Resistance**: 48 bytes of entropy provide excellent collision resistance

### Validation Security
- **Database Constraints**: Unique indexes on codes and tokens
- **Timing Attack Protection**: Consistent validation timing regardless of existence
- **Expiration Enforcement**: Server-side validation of expiration dates
- **Single Use**: Automatic marking as used prevents replay attacks

## Integration Points

### Registration Process
The invite system integrates with ASP.NET Core Identity registration:

1. **Registration Form**: Includes invite code field when `EnableInviteOnly` is true
2. **Validation**: Checks invite validity before user creation
3. **Usage Tracking**: Marks invite as used and records the new user
4. **Error Handling**: Provides clear feedback for invalid invites

### Email Integration
When email invites are enabled:
1. **Token Generation**: Creates secure URL-safe tokens
2. **Email Dispatch**: Sends invitation emails with registration links
3. **Link Validation**: Validates tokens from email links
4. **Pre-filled Registration**: Auto-populates email from invitation

### Database Integration
- **Foreign Key Relationships**: Links invites to creating and using users
- **Indexes**: Optimized for common query patterns
- **Cascade Behavior**: Appropriate deletion behavior for data integrity

## Usage Patterns

### Basic Invite Code Flow
```csharp
// Admin creates invite code
var invite = await inviteService.GenerateInviteCodeAsync(adminId);
// Share code: invite.Code

// User registers with code
var validInvite = await inviteService.ValidateInviteCodeAsync(userProvidedCode);
if (validInvite != null)
{
    // Proceed with registration
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await inviteService.UseInviteCodeAsync(userProvidedCode, user.Id);
    }
}
```

### Email Invite Flow
```csharp
// Admin creates email invite
var emailInvite = await inviteService.GenerateEmailInviteAsync("user@example.com", adminId);
// Send email with registration link including emailInvite.InviteToken

// User clicks email link and registers
var validInvite = await inviteService.ValidateEmailInviteAsync(tokenFromUrl);
if (validInvite != null)
{
    // Pre-fill email and proceed with registration
    // Mark as used after successful registration
    await inviteService.UseEmailInviteAsync(tokenFromUrl, user.Id);
}
```

## Maintenance Operations

### Cleanup Expired Invites
```csharp
// Run periodically to clean up expired invites
await inviteService.CleanupExpiredInvitesAsync();
```

### Admin Quota Management
```csharp
// Check if admin can create more codes
var canCreate = await inviteService.CanCreateMoreCodesAsync(adminId);
if (!canCreate)
{
    // Show quota exceeded message
}
```

## Error Handling

### Common Scenarios
- **Invalid Codes**: Returns null from validation methods
- **Expired Invites**: Checked in validation, excluded from active lists
- **Quota Exceeded**: Prevents creation of additional codes
- **Already Used**: Validation fails for previously used invites

### Logging
The service provides structured logging for:
- Invite generation events
- Validation attempts (successful and failed)
- Usage tracking
- Cleanup operations
- Security-relevant events

## Performance Considerations

### Database Indexing
- **Code/Token Lookups**: Unique indexes for fast validation
- **User Queries**: Indexes on creator and user relationships
- **Expiration Queries**: Optimized for cleanup and active filtering
- **Composite Indexes**: For multi-column query optimization

### Caching
- Consider caching validation results for frequently checked codes
- Cache admin quota information to reduce database queries
- Cache configuration settings to minimize repeated reads

## Troubleshooting

### Invite Codes Not Working
1. Verify `EnableInviteOnly` and `EnableInviteCodes` configuration
2. Check code expiration in database
3. Confirm code hasn't been used already
4. Validate database constraints and indexes

### Email Invites Not Functioning
1. Check `EnableEmailInvites` configuration
2. Verify email service configuration
3. Validate token generation and URL construction
4. Check email delivery and spam filters

### Performance Issues
1. Review database indexes for query optimization
2. Monitor cleanup operation frequency
3. Consider pagination for large invite lists
4. Implement caching for frequently accessed data

### Security Concerns
1. Regular cleanup of expired invites
2. Monitor for unusual creation patterns
3. Validate admin permissions for invite generation
4. Review audit logs for security events