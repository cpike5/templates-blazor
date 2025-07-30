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

## Setup

1. Update connection string in `appsettings.json`
2. Set admin email in `appsettings.json`
3. Run `dotnet ef database update`
4. Run `dotnet run`
5. Register using the admin email - you'll automatically get Administrator role

## Navigation Configuration

Edit `appsettings.json` to configure menu items:

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

## Tech Stack

- .NET 8, Blazor Server
- Entity Framework Core, SQL Server
- ASP.NET Core Identity
- Bootstrap 5, Font Awesome 6
- Serilog

## Adding New Roles

Add role names to `GetApplicationRoles()` in `Program.cs`. They'll be seeded automatically on startup.