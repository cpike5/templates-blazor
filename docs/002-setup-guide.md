# Blazor Template - Setup Guide

## Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server Express LocalDB
- Visual Studio, Visual Studio Code, or JetBrains Rider (optional)
- Git (for cloning)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/cpike5/templates-blazor.git
cd templates-blazor/src/blazor-template
```

### 2. Configure Database Connection

Edit `appsettings.json` or create `appsettings.Local.json` (ignored by git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=blazor_template;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Connection String Examples:**
- **LocalDB**: `Server=(localdb)\\mssqllocaldb;Database=blazor_template;Trusted_Connection=true;MultipleActiveResultSets=true`
- **SQL Server Express**: `Server=localhost\\SQLEXPRESS;Database=blazor_template;Trusted_Connection=true;MultipleActiveResultSets=true`
- **Docker SQL Server**: `Server=localhost,1433;Database=blazor_template;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true;`

### 3. Configure Admin Email

Edit `appsettings.json`:
```json
{
  "Site": {
    "Administration": {
      "AdminEmail": "your-admin@email.com"
    }
  }
}
```

### 4. Enable Setup Mode (Development)

For development, ensure setup mode is enabled. This is already configured in `appsettings.Development.json`:

```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": true,
      "EnableGuestUser": true
    }
  }
}
```

### 5. Apply Database Migrations

```bash
# From the src/blazor-template directory
dotnet ef database update
```

### 6. Run the Application

```bash
# From the src/blazor-template directory
dotnet run

# Or with hot reload for development
dotnet watch run
```

### 7. Register Admin User

- Navigate to `https://localhost:7001/Account/Register` (or your configured URL)
- Register using the admin email configured in step 3
- The first-time setup service will automatically assign the Administrator role
- **Note**: Guest user credentials will be displayed in the console if enabled

## Initial System Setup

The application includes an automated **First-Time Setup Service** that handles initial configuration:

### Automatic Setup Features
- **Role Creation**: Automatically creates application roles (Administrator, User, Guest)
- **Admin Assignment**: Assigns Administrator role to user with configured email
- **Guest User Creation**: Creates demo user account with secure generated password
- **Database Seeding**: Seeds essential data on first run

### Setup Mode Configuration
Setup mode is controlled by `Site.Setup.EnableSetupMode` in configuration:
- **Development**: Enabled by default in `appsettings.Development.json`
- **Production**: Should be disabled after initial setup

### Guest User System
When `EnableGuestUser` is enabled:
- Creates guest user with email from configuration (default: guest@test.com)
- Generates cryptographically secure random password (16 characters)
- Displays credentials prominently in console and logs
- Creates and assigns 'Guest' role automatically
- Account is email-confirmed and lockout-disabled for immediate demo use

## Feature Configuration

### Invite System Setup

The template includes a comprehensive invite system with two mechanisms:

#### Enable Invite-Only Registration

Edit `appsettings.json`:
```json
{
  "Site": {
    "Setup": {
      "InviteOnly": {
        "EnableInviteOnly": true,
        "EnableEmailInvites": true,
        "EnableInviteCodes": true,
        "DefaultCodeExpirationHours": 24,
        "DefaultEmailInviteExpirationHours": 72,
        "MaxActiveCodesPerAdmin": 50
      }
    }
  }
}
```

**Invite Features:**
- **Invite Codes**: 8-character alphanumeric codes for easy sharing
- **Email Invites**: Secure token-based email invitations
- **Admin Management**: Full admin interface at `/Admin/Invites`
- **Configurable Expiration**: Set custom expiration times
- **Usage Tracking**: Monitor invite usage and statistics

### Role Management System

The application includes advanced role management with permissions:

#### Default Roles
- **Administrator**: Full system access
- **User**: Standard user access
- **Guest**: Limited demo access (when guest user enabled)

#### Adding Custom Roles
1. Roles are automatically created from the `UserRoles` configuration
2. Update `ConfigurationOptions.cs` or override in `appsettings.json`
3. Restart with setup mode enabled to create new roles
4. Assign permissions through the admin interface

#### Role Permissions
The system includes granular permissions that can be assigned to roles:
- Configure through the admin role management pages
- Permissions are stored in the database and cached for performance

### Settings Management

The application includes an encrypted settings system:

#### Settings Configuration
```json
{
  "Site": {
    "Settings": {
      "EnableBackups": true,
      "EncryptionKey": "your-production-encryption-key",
      "CacheSettings": {
        "Enabled": true,
        "ExpirationMinutes": 30
      }
    }
  }
}
```

**Important**: Change the default `EncryptionKey` in production environments.

### Theme System

The application includes a server-side theme system with persistent storage:

#### Available Themes
- **Light Theme**: Default professional theme
- **Dark Theme**: Dark mode for reduced eye strain
- **Blue Theme**: Professional blue color scheme
- **Green Theme**: Nature-inspired green theme

#### Theme Features
- **Server-side rendering**: No theme flash on page load
- **User preferences**: Persistent theme selection per user
- **Database storage**: Themes stored in user profiles
- **Fallback support**: localStorage backup for non-authenticated users

## External Authentication

### Google OAuth Setup (Optional)

#### 1. Create Google OAuth Credentials

- Go to [Google Cloud Console](https://console.cloud.google.com/)
- Create or select a project
- Enable Google Identity services
- Create OAuth 2.0 credentials
- Add authorized redirect URIs (e.g., `https://localhost:7001/signin-google`)

#### 2. Configure Credentials

Create `appsettings.Local.json` (not tracked in git) or add to `appsettings.json`:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

#### 3. Test OAuth Integration

- Restart the application
- Navigate to login page
- "Sign in with Google" button should appear
- Test the authentication flow

## Navigation Configuration

The navigation system dynamically builds menus from configuration with role-based filtering:

### Basic Navigation Structure

Edit `appsettings.json` to customize menu items:

```json
{
  "Navigation": {
    "Items": [
      {
        "Id": "home",
        "Title": "Home",
        "Href": "/",
        "Icon": "fas fa-home",
        "RequiredRoles": [],
        "Order": 10
      },
      {
        "Id": "admin-section",
        "Title": "Administration",
        "Icon": "fas fa-cog",
        "RequiredRoles": ["Administrator"],
        "Order": 20,
        "Children": [
          {
            "Id": "users",
            "Title": "Users",
            "Href": "/users",
            "Icon": "fas fa-users",
            "RequiredRoles": ["Administrator"],
            "Order": 10
          },
          {
            "Id": "admin-dashboard",
            "Title": "Dashboard",
            "Href": "/admin",
            "Icon": "fas fa-tachometer-alt",
            "RequiredRoles": ["Administrator"],
            "Order": 15
          },
          {
            "Id": "invites",
            "Title": "Invite Management",
            "Href": "/Admin/Invites",
            "Icon": "fas fa-envelope-open-text",
            "RequiredRoles": ["Administrator"],
            "Order": 20
          }
        ]
      }
    ]
  }
}
```

### Navigation Features
- **Role-based filtering**: Items automatically hidden based on user roles
- **Hierarchical structure**: Support for nested menu items
- **External links**: Use `IsExternal: true` for external URLs
- **Font Awesome icons**: Full Font Awesome 6 icon support
- **Flexible ordering**: Control menu item order with `Order` property

### Anonymous User Navigation
Use `"RequiredRoles": ["Anonymous"]` for items visible only to non-authenticated users (login, register).

## Development vs Production

### Development Environment
- **Detailed logging**: Verbose logging in `appsettings.Development.json`
- **Setup mode enabled**: Automatic role seeding and admin assignment
- **Guest user enabled**: Demo account with console-displayed credentials
- **EF migrations page**: Shows detailed error pages for database issues
- **No-op email sender**: Displays confirmation links in browser console
- **Hot reload**: Use `dotnet watch run` for automatic rebuilds

### Production Environment

#### Security Configuration
```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": false,
      "EnableGuestUser": false
    },
    "Settings": {
      "EncryptionKey": "your-secure-production-encryption-key"
    }
  }
}
```

#### Required Production Setup
- **Disable setup mode**: Set `EnableSetupMode: false`
- **Configure secure encryption key**: Replace default development key
- **Email service**: Configure proper email service for Identity confirmations
- **HTTPS certificates**: Set up SSL certificates for secure connections
- **Connection strings**: Use secure, environment-specific database connections
- **Logging levels**: Reduce to Warning/Error levels
- **Rate limiting**: Configure API rate limiting for security

## Essential Commands

### Development Commands
```bash
# Navigate to project directory
cd src/blazor-template

# Run the application
dotnet run

# Run with hot reload (recommended for development)
dotnet watch run

# Run with specific environment
dotnet run --environment Development

# Build the application
dotnet build

# Clean build artifacts
dotnet clean
```

### Database Commands
```bash
# Apply pending migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script

# Drop database (development only)
dotnet ef database drop
```

## Logging Configuration

The application uses Serilog for structured logging:

### Production Logging (`appsettings.json`)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/application-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Development Logging (`appsettings.Development.json`)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

## Database Architecture

The application uses Entity Framework Core with the following key entities:

### Core Entities
- **ApplicationUser**: Extended Identity user with theme preferences
- **UserActivity**: Activity tracking and audit logging
- **InviteCode**: Short-form invitation codes
- **EmailInvite**: Email-based invitations with secure tokens
- **RefreshToken**: JWT refresh token storage (if API enabled)
- **ApplicationSetting**: Encrypted application settings storage
- **RolePermission**: Granular role-based permissions

### Migration History
Recent migrations include:
- **AddRolePermissions**: Role-based permission system
- **AddApplicationSettingsTables**: Encrypted settings storage
- **Initial migration**: Core Identity and custom entities

## Troubleshooting

### Database Issues

#### Connection Problems
```bash
# Test connection string
dotnet ef database update --verbose

# Check SQL Server is running
# Windows: services.msc -> SQL Server services
# Docker: docker ps (check container status)
```

**Common Solutions:**
- Verify SQL Server/LocalDB is running
- Check connection string format and server name
- Ensure database user has appropriate permissions
- For LocalDB: Try `(localdb)\mssqllocaldb` as server name

#### Migration Failures
```bash
# View migration status
dotnet ef migrations list

# Apply specific migration
dotnet ef database update MigrationName

# Reset to specific migration
dotnet ef database update PreviousMigrationName
```

### Authentication Issues

#### Admin Role Assignment
- Admin email must match exactly (case-sensitive)
- Ensure `EnableSetupMode` is true for automatic assignment
- Check console logs for role assignment messages
- Verify admin email is set in `Site.Administration.AdminEmail`

#### Guest User Not Created
- Verify `EnableGuestUser` is true in configuration
- Check console output for generated credentials
- Ensure setup mode is enabled
- Review application startup logs

### Navigation Problems

#### Menu Items Not Showing
- Verify user has required roles assigned
- Check `RequiredRoles` array in navigation configuration
- Empty `RequiredRoles` means visible to all authenticated users
- Use `["Anonymous"]` for items visible only to non-authenticated users

#### Role-based Access Issues
- Confirm user roles in admin dashboard or database
- Verify role names match exactly (case-sensitive)
- Check that roles are properly seeded on startup

### Application Startup Issues

#### Setup Mode Problems
```bash
# Enable verbose logging to see setup process
# Check appsettings.Development.json has EnableSetupMode: true
```

#### Service Registration Errors
- Verify all required services are registered in `Program.cs`
- Check for circular dependencies in service constructors
- Review dependency injection configuration

### Performance Issues

#### Slow Database Queries
- Enable EF Core query logging in development
- Review database indexes and query execution plans
- Consider adding appropriate indexes for custom queries

#### Theme Loading Problems
- Check if theme service is properly registered
- Verify user theme preferences are stored correctly
- Clear browser localStorage if using theme fallbacks

## Advanced Configuration

### Environment-Specific Settings

Create environment-specific configuration files:
- `appsettings.Local.json` (ignored by git, for local development)
- `appsettings.Staging.json` (for staging environment)
- `appsettings.Production.json` (for production)

### Custom Role Management
1. Update `ConfigurationOptions.Administration.UserRoles`
2. Add role permissions through admin interface
3. Configure navigation items with appropriate `RequiredRoles`
4. Restart application with setup mode enabled to create roles

### Invite System Customization
- Adjust expiration times in configuration
- Modify code/token generation algorithms in services
- Customize email templates (when email invites are implemented)
- Configure admin quotas and rate limiting

## Security Best Practices

### Configuration Security
- Use secure encryption keys in production
- Store sensitive configuration in environment variables
- Use secure connection strings with appropriate authentication
- Enable HTTPS in production environments

### Database Security
- Use least-privilege database accounts
- Enable SQL Server encryption (TDE) for sensitive data
- Regular database backups with secure storage
- Monitor database access logs

### Application Security
- Disable setup mode in production
- Configure appropriate rate limiting
- Review and audit user permissions regularly
- Monitor application logs for security events

## Getting Help

### Documentation
- Review `/docs` folder for detailed component documentation
- Check specific feature documentation (invite system, theme system, etc.)
- Review CLAUDE.md for development guidelines

### Development Resources
- ASP.NET Core Identity documentation
- Entity Framework Core documentation
- Blazor Server documentation
- Bootstrap 5 component documentation