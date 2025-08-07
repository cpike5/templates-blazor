# Blazor Server Template

A comprehensive Blazor Server template for rapid prototyping with authentication, role-based navigation, invite systems, JWT API, and advanced user management.

**GitHub Repository**: https://github.com/cpike5/templates-blazor

## What's Included

### Core Features
- **ASP.NET Core Identity** with Entity Framework and custom user extensions
- **Role-based access control** with configurable roles and permissions
- **Dynamic navigation system** from appsettings.json with role filtering
- **Theme system** with server-side rendering and persistent preferences
- **Invite-only registration** with codes and email invitations
- **JWT API** with refresh tokens and standardized responses
- **User activity tracking** and comprehensive audit logging
- **Rate limiting** and API security middleware
- **Google OAuth integration** for external authentication
- **Guest/demo user system** with secure auto-generated credentials

### Architecture
- **Blazor Server** with .NET 8 and Entity Framework Core
- **SQL Server** database with optimized indexing
- **Serilog** structured logging
- **Bootstrap 5** responsive design
- **Font Awesome 6** iconography
- **Swagger/OpenAPI** documentation
- **First-time setup service** with automated database seeding

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

## Guest User / Demo Mode

The application includes a secure guest user system for demonstrations and public access:

### Features

- **Secure Password Generation**: Cryptographically secure random passwords generated at startup
- **No Hard-coded Credentials**: Eliminates security risks from exposed passwords in configuration
- **Console Logging**: Generated credentials displayed prominently for easy access
- **Automatic Role Management**: Creates and assigns "Guest" role automatically
- **Demo-Ready Account**: Email confirmed, lockout disabled for immediate use

### Configuration

Enable guest user in `appsettings.json`:

```json
{
  "Site": {
    "Setup": {
      "EnableGuestUser": true,
      "GuestUser": {
        "Email": "guest@test.com"
      }
    }
  }
}
```

### Usage

1. **Enable guest mode** by setting `EnableGuestUser: true`
2. **Start the application** - guest account is created automatically
3. **Check console output** for generated credentials:
   ```
   === GUEST USER CREDENTIALS ===
   Email: guest@test.com
   Password: [randomly generated secure password]
   ===============================
   ```
4. **Login using displayed credentials** for demo access

### Security Features

- **Dynamic Password Generation**: New secure password generated each startup
- **ASP.NET Identity Compliance**: 16-character passwords with uppercase, lowercase, digits, and special characters
- **Cryptographic Security**: Uses `RandomNumberGenerator` for secure random generation
- **No Configuration Exposure**: Passwords never stored in config files
- **Role-Based Access**: Guest role can be configured with limited permissions

### Development vs Production

**Development Environment**:
- Guest user enabled by default in `appsettings.Development.json`
- Credentials logged to console and application logs
- Suitable for local testing and development

**Production Environment**:
- Guest user disabled by default in `appsettings.json`
- Enable only for public demonstrations
- Consider additional security measures for public-facing deployments

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

- **NavigationService** - Builds role-filtered menus from configuration
- **UserRoleService** - Manages user role assignments and queries  
- **UserManagementService** - Complete user operations and management
- **ThemeService** - Server-side theme switching with persistence
- **JwtTokenService** - JWT generation, validation, and refresh token management
- **InviteService** - Invitation code and email invite generation/validation
- **AdminRoleService** - Admin-specific role operations
- **FirstTimeSetupService** - Handles initial application setup and seeding
- **DataSeeder** - Seeds database with roles, admin user, and initial data

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

## API Endpoints

The template includes a comprehensive REST API with JWT authentication:

### Authentication Endpoints
- `POST /api/auth/login` - User authentication with JWT response
- `POST /api/auth/refresh` - Refresh expired access tokens  
- `POST /api/auth/logout` - User logout with token revocation
- `GET /api/auth/profile` - Current user profile information
- `POST /api/auth/revoke` - Revoke refresh tokens

### User Management Endpoints  
- `GET /api/users` - List users with filtering and pagination
- `GET /api/users/{id}` - Get specific user details
- `PUT /api/users/{id}` - Update user information
- `POST /api/users/{id}/roles` - Assign roles to users
- `DELETE /api/users/{id}/roles/{role}` - Remove roles from users

## Documentation

Comprehensive documentation is available in the `/docs` folder:

- **[000-quickstart-reference.md](docs/000-quickstart-reference.md)** - Quick start guide and overview
- **[002-navigation-service-docs.md](docs/002-navigation-service-docs.md)** - Navigation system configuration
- **[005-theme-service.md](docs/005-theme-service.md)** - Theme system architecture and usage
- **[007-invite-system.md](docs/007-invite-system.md)** - Comprehensive invite system guide
- **[008-jwt-api-system.md](docs/008-jwt-api-system.md)** - JWT API authentication and endpoints
- **[009-database-architecture.md](docs/009-database-architecture.md)** - Database models and relationships
- **[010-configuration-guide.md](docs/010-configuration-guide.md)** - Complete configuration reference

## Development vs Production

### Development
- Detailed logging configured in `appsettings.Development.json`
- Entity Framework migrations page for debugging
- No-op email sender (shows links in browser)
- Setup mode enabled for easy testing
- Guest user enabled for demos
- Swagger documentation enabled
- Rate limiting disabled

### Production
- Warning-level logging only
- Proper email sender configuration required
- HTTPS certificates and security headers
- Setup mode disabled after initial deployment
- Guest user disabled (unless needed for demos)
- Swagger documentation disabled
- Rate limiting enabled with appropriate limits
- Secure JWT signing keys (use Azure Key Vault or similar)
- Database connection strings secured