# Blazor Server Template - Quickstart Reference

## Template Overview
Blazor Server template with ASP.NET Core Identity, role-based access control, and configurable navigation. Built on .NET 8 with Entity Framework Core and SQL Server.

**GitHub Repository**: https://github.com/cpike5/templates-blazor

## Core Features Ready Out-of-Box
- **Authentication**: ASP.NET Core Identity with Google OAuth support
- **Authorization**: Role-based access control with configurable roles
- **Navigation**: Dynamic menu system from appsettings.json with role filtering
- **Theming**: Built-in theme switching service and UI components
- **User Management**: Admin interface for user and role management
- **Guest System**: Secure demo user with auto-generated credentials
- **Database**: Entity Framework Core with SQL Server and migrations

## Essential Services Available
- `NavigationService` - Role-filtered menu generation
- `UserRoleService` - Role assignment and queries
- `UserManagementService` - User operations
- `ThemeService` - Theme switching
- `JwtTokenService` - JWT token handling
- `FirstTimeSetupService` - Initial app setup and seeding

## Quick Setup for New Project

### 1. Configuration (`appsettings.json`)
```json
{
  "Site": {
    "Administration": {
      "AdminEmail": "your-admin@email.com"
    },
    "Setup": {
      "EnableSetupMode": true,  // Disable after first setup
      "EnableGuestUser": true   // For demo purposes
    }
  },
  "Navigation": {
    "Items": [
      // Define your menu structure with role-based access
    ]
  }
}
```

### 2. Database Setup
```bash
# From src/blazor-template directory
dotnet ef database update
```

### 3. Run Application
```bash
dotnet run  # or dotnet watch run for hot reload
```

## Key Directories for New Project Development

### Add Your Features
- `Components/Pages/` - Your main application pages
- `Services/` - Your business logic services
- `Controllers/` - Your API endpoints
- `Data/` - Your data models and DbContext extensions

### Extend Existing
- `Components/Admin/` - Additional admin functionality
- `Components/Shared/` - Reusable components
- `Configuration/` - Additional configuration models
- `DTO/` - Data transfer objects for APIs

## Common Customizations

### Navigation Menu
Edit `appsettings.json` Navigation section:
```json
"Navigation": {
  "Items": [
    {
      "Name": "Your Feature",
      "Href": "/your-feature",
      "Icon": "bi-star",
      "Order": 100,
      "Roles": ["User"],
      "IsVisible": true
    }
  ]
}
```

### New Roles
Add to `Data/DataSeeder.cs` or use admin interface after startup.

### Custom Pages
Create in `Components/Pages/` with `@rendermode InteractiveServer` for interactive components.

### Database Models
Add to `Data/ApplicationDbContext.cs` and create migration:
```bash
dotnet ef migrations add YourModelName
dotnet ef database update
```

## Critical Blazor Server Patterns

### JavaScript Interop
```csharp
// Only call JS after render, never during prerendering
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JSRuntime.InvokeAsync<string>("method");
        StateHasChanged();
    }
}
```

### Interactive Components
```razor
@rendermode InteractiveServer
@* Required for components with @onclick, form submissions, etc. *@
```

## Development Commands
```bash
# Development
dotnet run                          # Start app
dotnet watch run                    # Hot reload
dotnet build                        # Build solution
dotnet clean                        # Clean build artifacts

# Database
dotnet ef database update           # Apply migrations
dotnet ef migrations add Name       # Create migration
dotnet ef migrations remove         # Remove last migration
```

## Getting Started
1. Clone or fork the repository: `git clone https://github.com/cpike5/templates-blazor.git`
2. Navigate to project: `cd templates-blazor/src/blazor-template`
3. Configure your settings in `appsettings.json`
4. Run database setup: `dotnet ef database update`
5. Start development: `dotnet watch run`

## Next Steps for Your Project
1. Define your data models and add to `ApplicationDbContext`
2. Create database migrations for your models
3. Configure navigation menu in `appsettings.json`
4. Build your pages in `Components/Pages/`
5. Add business logic services in `Services/`
6. Create API endpoints in `Controllers/` if needed
7. Customize roles and permissions as required

## Security Notes
- Admin user auto-assigned based on `Site:Administration:AdminEmail`
- Guest credentials auto-generated and logged to console
- Role-based navigation filtering automatic
- Authentication required by default (configurable)
- No hardcoded passwords or secrets