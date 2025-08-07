# Demo User Functionality Overview

## Current Implementation

The Blazor template includes a guest/demo user system designed to allow public demonstration of the application without requiring user registration. This document outlines the current implementation, security considerations, and recommended improvements.

## Features

### Configuration System

The guest user functionality is controlled through the application configuration system:

**Configuration Location**: `appsettings.json` / `appsettings.Development.json`

```json
{
  "Site": {
    "Setup": {
      "EnableGuestUser": false,
      "GuestUser": {
        "Email": "guest@test.com",
        "Password": ">a<cy2*U5MFjedHe"
      }
    }
  }
}
```

**Configuration Classes**:
- `ConfigurationOptions.cs:24` - `EnableGuestUser` boolean flag
- `ConfigurationOptions.cs:25` - `GuestUser` account configuration
- `ConfigurationOptions.cs:27` - `GuestUserAccount` class definition

### Automatic Account Provisioning

When `EnableGuestUser` is enabled, the system automatically:

1. **Creates Guest Account** - During database seeding (`DataSeeder.cs:59-126`)
2. **Assigns Guest Role** - Creates and assigns "Guest" role if it doesn't exist
3. **Configures Account Properties**:
   - Email confirmed: `true`
   - Lockout enabled: `false`
   - Username: Uses configured email address

### Admin Interface Integration

The guest user system integrates with the admin interface:

- **Settings Page**: Toggle switch for "Guest Access" (`Settings.razor:691`)
- **Runtime Control**: Ability to enable/disable guest functionality through UI
- **Visual Indicator**: Switch shows current guest access status

## Technical Implementation

### Database Seeding Process

The `DataSeeder` class handles guest user creation:

```csharp
// Create the Guest User if enabled
if (config.Value.Setup.EnableGuestUser)
{
    var guest = config.Value.Setup.GuestUser;
    var guestUser = await userManager.FindByEmailAsync(guest.Email);
    
    if (guestUser == null)
    {
        // Create new guest user account
        var newUser = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = guest.Email,
            Email = guest.Email,
            EmailConfirmed = true,
            LockoutEnabled = false
        }, guest.Password);
        
        // Create and assign Guest role
        var guestRoleName = "Guest";
        if (!await roleManager.RoleExistsAsync(guestRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(guestRoleName));
        }
        
        // Assign user to role
        await userManager.AddToRoleAsync(createdUser, guestRoleName);
    }
}
```

### Service Integration

The guest user system integrates with:

- **FirstTimeSetupService** - Triggers database seeding during application startup
- **UserRoleService** - Manages role assignments including Guest role
- **NavigationService** - (Currently no Guest role filtering implemented)

## Current Limitations

### Security Concerns

1. **Credential Exposure**: Password stored in plain text in configuration files
2. **No Access Restrictions**: Guest role not properly integrated with navigation/authorization
3. **Session Management**: No special handling for guest user sessions
4. **Data Persistence**: Guest actions persist in database without cleanup

### Functional Limitations

1. **Navigation**: No role-based filtering for Guest users in navigation
2. **UI Indicators**: No visual indication when using demo mode
3. **Feature Restrictions**: No limitations on guest user capabilities
4. **Cleanup**: No automatic cleanup of guest-created data

## Usage Instructions

### Development Environment

1. Guest user is enabled by default in `appsettings.Development.json`
2. Access with credentials: `guest@test.com` / `>a<cy2*U5MFjedHe`
3. Guest account is created automatically on application startup

### Production Environment

1. Guest user is disabled by default in `appsettings.json`
2. Enable through admin settings or configuration file
3. Ensure security measures are in place before enabling

## Files Involved

### Core Implementation
- `DataSeeder.cs:59-126` - Guest account creation logic
- `ConfigurationOptions.cs:24-31` - Configuration classes
- `FirstTimeSetupService.cs` - Triggers seeding process

### Configuration Files
- `appsettings.json:12-16` - Production configuration
- `appsettings.Development.json:5-9` - Development configuration

### Admin Interface
- `Settings.razor:691` - Guest access toggle in admin settings

### Service Registration
- `Program.cs:22` - Reads EnableSetupMode configuration
- `Extensions/ServiceCollectionExtensions.cs` - Service registration

## Next Steps

See the accompanying security analysis and implementation roadmap documents for detailed information about recommended improvements and security enhancements.