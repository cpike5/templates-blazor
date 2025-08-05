# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Blazor Server template for rapid prototyping with ASP.NET Core Identity, role-based access control, and configurable navigation. Built on .NET 8 with Entity Framework Core and SQL Server.

## Essential Commands

### Development
```bash
# Run the application (from src/blazor-template directory)
dotnet run

# Run in development mode with detailed logging
dotnet run --environment Development

# Run with hot reload
dotnet watch run
```

### Database
```bash
# Apply database migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove
```

### Build and Test
```bash
# Build the solution
dotnet build

# Build for release
dotnet build --configuration Release

# Clean build artifacts
dotnet clean
```

## Architecture Overview

### Core Services Architecture
- **NavigationService**: Builds role-filtered navigation menus from appsettings.json configuration
- **UserRoleService**: Manages user role assignments and queries (src/blazor-template/Services/UserRoleService.cs)
- **FirstTimeSetupService**: Handles initial application setup and database seeding
- **DataSeeder**: Seeds database with roles and admin user assignments

### Configuration System
Configuration is centralized through `ConfigurationOptions` (src/blazor-template/Configuration/ConfigurationOptions.cs):
- Site administration settings (admin email, user roles)
- Setup mode controls (first-time setup, guest user)
- Navigation structure defined in appsettings.json

### Authentication & Authorization
- ASP.NET Core Identity with Entity Framework stores
- Role-based access control with configurable roles
- Google OAuth integration (optional)
- Guest user support for demo purposes

### Navigation System
Navigation is dynamically generated from appsettings.json:
- Hierarchical menu structure with role-based filtering
- Icons, ordering, and visibility controls
- Automatic role filtering in NavigationService

## Key Configuration

### Setup Mode
The application uses `Site:Setup:EnableSetupMode` in appsettings.json to control first-time initialization:
- When enabled: automatically seeds roles and assigns admin privileges
- Should be disabled in production after initial setup

### Admin User Assignment
Admin privileges are automatically assigned to users who register with the email specified in `Site:Administration:AdminEmail`.

### Navigation Configuration
Navigation items are defined in the `Navigation:Items` section of appsettings.json with role-based access control.

## Database Context

The `ApplicationDbContext` (src/blazor-template/Data/ApplicationDbContext.cs) extends IdentityDbContext and manages:
- ASP.NET Identity tables (Users, Roles, UserRoles)
- Custom application entities
- Database seeding through DataSeeder

## Development Notes

- The project uses Serilog for structured logging
- Entity Framework migrations are stored in Data/Migrations
- Component structure follows Blazor Server conventions
- Layout components are in Components/Layout
- Identity pages are customized in Components/Account

## Working Directory
The main project is located in `src/blazor-template/` - most dotnet commands should be run from this directory.