# Blazor Template - Setup Guide

## Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server Express
- Visual Studio or Visual Studio Code

## Initial Setup

### 1. Clone or Download the Template

### 2. Update Database Connection

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=blazor_template;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Set Admin Email

Edit `appsettings.json`:
```json
{
  "Site": {
    "AdminEmail": "your-admin@email.com"
  }
}
```

### 4. Create Database

```bash
dotnet ef database update
```

### 5. Run the Application

```bash
dotnet run
```

### 6. Register Admin User

- Navigate to `/Account/Register`
- Register using the admin email from step 3
- You'll automatically receive Administrator role

## Google OAuth Setup (Optional)

### 1. Create Google OAuth Credentials

- Go to [Google Cloud Console](https://console.cloud.google.com/)
- Create or select a project
- Enable Google+ API
- Create OAuth 2.0 credentials

### 2. Configure Credentials

Add to `appsettings.json`:
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

## Navigation Configuration

Edit the `Navigation` section in `appsettings.json` to customize your menu structure:

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
      }
    ]
  }
}
```

Navigation items are automatically filtered by user roles.

## Development vs Production

### Development
- Uses detailed logging (set in `appsettings.Development.json`)
- Shows Entity Framework migrations page for errors
- No-op email sender (shows confirmation links in browser)

### Production
- Configure proper email sender for Identity
- Set up proper error handling
- Configure HTTPS certificates
- Review logging levels

## Logging Configuration

The application uses Serilog for logging. Configuration is in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

Development logging (in `appsettings.Development.json`):
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

## Common Issues

### Database Connection
- Ensure SQL Server is running
- Verify connection string is correct
- Check if the database user has proper permissions

### Admin Role Assignment
- Admin email must match exactly (case-sensitive)
- Role assignment happens after user registration
- Check logs for role assignment messages

### Navigation Not Showing
- Verify `RequiredRoles` configuration
- Check user is assigned to correct roles
- Empty `RequiredRoles` array means visible to all users

## Adding New Roles

1. Add role name to `GetApplicationRoles()` in `Program.cs`
2. Restart application (roles are seeded on startup)
3. Assign users to new role via `UserRoleService`