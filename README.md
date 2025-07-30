# Blazor Template

A Blazor Server template for rapid prototyping with authentication, role-based navigation, and basic user management.

## What's Included

- ASP.NET Core Identity with Entity Framework
- Role-based access control (Administrator, User roles)
- Google OAuth support
- Configurable navigation system via appsettings.json
- Responsive sidebar layout
- User management services
- Serilog logging
- First-time setup service with automatic database seeding

## Live Demo

Coming Soon

## Setup

### Initial Configuration

1. **Update connection string** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=blazor_template;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
     }
   }
   ```

2. **Set admin email** in `appsettings.json`:
   ```json
   {
     "Site": {
       "Administration": {
         "AdminEmail": "your-admin@email.com"
       }
     }
   }
   ```

3. **Enable setup mode** in `appsettings.json` (enabled by default):
   ```json
   {
     "SetupMode": true
   }
   ```

### Database Setup

4. **Run Entity Framework migrations**:
   ```bash
   dotnet ef database update
   ```

5. **Run the application**:
   ```bash
   dotnet run
   ```

6. **Register admin user**:
   - Navigate to `/Account/Register`
   - Register using the admin email from step 2
   - You'll automatically receive Administrator role through the data seeding process

### First-Time Setup Process

The application includes an automated first-time setup service that:

- **Automatically seeds application roles** defined in `ConfigurationOptions.Administration.UserRoles`
- **Assigns admin privileges** to the user with the configured admin email
- **Runs on application startup** when `SetupMode` is enabled
- **Safely handles multiple runs** without duplicating data

The setup process is managed by:
- `FirstTimeSetupService` - Orchestrates the setup process
- `DataSeeder` - Handles database seeding operations
- Configuration through `appsettings.json`

To disable automatic setup after initial configuration, set `"SetupMode": false` in `appsettings.json`.

## Navigation Configuration

Edit the `Navigation` section in `appsettings.json` to configure menu items:

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
        "Title": "Admin",
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
          }
        ]
      }
    ]
  }
}
```

Navigation items are filtered by user roles automatically. Empty `RequiredRoles` means visible to everyone.

## External Authentication

Add Google OAuth credentials to `appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

## Key Services

- `NavigationService` - Builds role-filtered menus
- `UserRoleService` - Manages user role assignments
- `IdentityUserAccessor` - Simplified user access in components
- `FirstTimeSetupService` - Handles initial application setup
- `DataSeeder` - Seeds database with roles and admin user

## Tech Stack

- .NET 8, Blazor Server
- Entity Framework Core, SQL Server
- ASP.NET Core Identity
- Bootstrap 5, Font Awesome 6
- Serilog

## Role Management

### Default Roles

Application roles are defined in `ConfigurationOptions.Administration.UserRoles` and include:
- `Administrator` - Full system access
- `User` - Basic user access

### Adding New Roles

1. Add role names to the `UserRoles` collection in `ConfigurationOptions`
2. Update your `appsettings.json` if needed
3. Restart application with `SetupMode: true` (roles are seeded automatically)
4. Assign users to new roles via `UserRoleService`

### Admin User Setup

The admin user is automatically assigned the Administrator role based on the email configured in `appsettings.json`. This happens during the data seeding process when:

1. A user registers with the configured admin email
2. The `DataSeeder` runs during application startup
3. The user is automatically assigned to the Administrator role

## Configuration Structure

```json
{
  "SetupMode": true,
  "Site": {
    "Administration": {
      "AdminEmail": "admin@example.com",
      "AdministratorRole": "Administrator",
      "UserRoles": ["Administrator", "User"]
    }
  }
}
```

## Development vs Production

### Development
- Uses detailed logging (configured in `appsettings.Development.json`)
- Shows Entity Framework migrations page for errors
- No-op email sender (shows confirmation links in browser)
- `SetupMode` typically enabled for development

### Production
- Configure proper email sender for Identity
- Set up proper error handling
- Configure HTTPS certificates
- Review logging levels
- **Important**: Set `SetupMode: false` in production after initial setup