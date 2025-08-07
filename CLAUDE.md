# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Blazor Server template for rapid prototyping with ASP.NET Core Identity, role-based access control, configurable navigation, and file management capabilities. Built on .NET 8 with Entity Framework Core and SQL Server.

**GitHub Repository**: https://github.com/cpike5/templates-blazor

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
# Build the solution (from root directory)
dotnet build src/templates-blazor.sln

# Build for release
dotnet build src/templates-blazor.sln --configuration Release

# Clean build artifacts
dotnet clean src/templates-blazor.sln
```

## Architecture Overview

### Core Services Architecture
- **NavigationService**: Builds role-filtered navigation menus from appsettings.json configuration
- **UserRoleService**: Manages user role assignments and queries (src/blazor-template/Services/UserRoleService.cs)
- **UserManagementService**: Comprehensive user operations and management
- **ThemeService**: Server-side theme switching with persistent user preferences
- **JwtTokenService**: JWT token generation, validation, and refresh token management
- **InviteService**: Invitation code and email invite generation and validation
- **AdminRoleService**: Admin-specific role operations and management
- **FirstTimeSetupService**: Handles initial application setup and database seeding
- **DataSeeder**: Seeds database with roles and admin user assignments
- **MediaManagementService**: File upload, storage, and management with access control
- **FileSecurityService**: File validation, security scanning, and access authorization
- **LocalFileStorageService**: Local disk-based file storage with organized folder structure

### Configuration System
Configuration is centralized through `ConfigurationOptions` (src/blazor-template/Configuration/ConfigurationOptions.cs):
- Site administration settings (admin email, user roles)
- Setup mode controls (first-time setup, guest user, invite-only registration)
- Navigation structure defined in appsettings.json
- JWT token configuration (signing keys, expiration, issuer/audience)
- API configuration (CORS, rate limiting, Swagger)
- Invite system configuration (expiration times, quotas)
- File management configuration (storage paths, security policies, upload limits)

### Authentication & Authorization
- ASP.NET Core Identity with Entity Framework stores
- Role-based access control with configurable roles
- JWT-based API authentication with refresh tokens
- Google OAuth integration (optional)
- Guest user support for demo purposes
- Invite-only registration system with codes and email invites

### Navigation System
Navigation is dynamically generated from appsettings.json:
- Hierarchical menu structure with role-based filtering
- Icons, ordering, and visibility controls
- External link support with proper target handling
- Automatic role filtering in NavigationService

## Key Configuration

### Setup Mode
The application uses `Site:Setup:EnableSetupMode` in appsettings.json to control first-time initialization:
- When enabled: automatically seeds roles and assigns admin privileges
- Should be disabled in production after initial setup

### Admin User Assignment
Admin privileges are automatically assigned to users who register with the email specified in `Site:Administration:AdminEmail`.

### Guest User System
The application includes a secure guest/demo user system:
- **Automatic Password Generation**: Cryptographically secure random passwords generated on startup
- **Security Enhancement**: No hard-coded passwords in configuration files
- **Credential Logging**: Generated credentials logged to console and application logs for easy access
- **Configuration**: Controlled via `Site:Setup:EnableGuestUser` in appsettings.json
- **Role Management**: Automatically creates and assigns "Guest" role
- **Account Properties**: Email confirmed, lockout disabled for demo purposes

When `EnableGuestUser` is enabled, the system:
1. Generates a secure random password meeting ASP.NET Identity requirements (16 characters with uppercase, lowercase, digits, and special characters)
2. Creates guest account with email from configuration (default: guest@test.com)  
3. Logs credentials prominently to console and application logs
4. Creates "Guest" role and assigns it to the guest user
5. Configures account for immediate use (email confirmed, no lockout)

### Navigation Configuration
Navigation items are defined in the `Navigation:Items` section of appsettings.json with role-based access control.

## Database Context

The `ApplicationDbContext` (src/blazor-template/Data/ApplicationDbContext.cs) extends IdentityDbContext and manages:
- ASP.NET Identity tables (Users, Roles, UserRoles) with custom ApplicationUser
- UserActivity - User action tracking and audit logging
- InviteCode - Short-form invitation codes for registration
- EmailInvite - Email-based invitation system
- RefreshToken - JWT refresh token storage and management
- MediaFile - File metadata, storage information, and access control
- MediaFileAccess - Granular file access permissions and tracking
- Comprehensive indexing for performance optimization
- Database seeding through DataSeeder

## Development Notes

- The project uses Serilog for structured logging
- Entity Framework migrations are stored in Data/Migrations
- Component structure follows Blazor Server conventions
- Layout components are in Components/Layout
- Identity pages are customized in Components/Account

### Blazor Server JavaScript Interop
**CRITICAL**: JavaScript interop calls cannot be made during server-side prerendering. Always use this pattern:

```csharp
// In services that use JSRuntime
public async Task InitializeAsync(bool afterRender = false)
{
    if (!afterRender)
    {
        // During prerendering, just set defaults
        return;
    }
    // JavaScript calls only after client rendering
    await _jsRuntime.InvokeAsync<string>('localStorage.getItem', 'key');
}

// In components
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await ServiceWithJSInterop.InitializeAsync(afterRender: true);
        StateHasChanged();
    }
}
```

**Never call JavaScript in:** `OnInitializedAsync`, `OnParametersSetAsync`, or during component construction.
**Always call JavaScript in:** `OnAfterRenderAsync` with `firstRender = true` check.

### Blazor Server Interactive Components
**CRITICAL**: Components that handle user interactions (clicks, form submissions) must include `@rendermode InteractiveServer` directive at the top of the .razor file. Without this directive, components render as static HTML and event handlers like `@onclick` will not work.

```razor
@rendermode InteractiveServer
@* Component content *@
```

**Static rendering symptoms:**
- Click events don't fire
- No console output from event handlers  
- Component appears but doesn't respond to user input

## Coding Guidelines

Follow the .NET development standards in `docs/guidelines/guidelines-dotnet-consolidated.md`:
- Structured logging with Serilog
- Module library organization patterns  
- Service layer with direct DbContext access
- DTO design and AutoMapper integration
- Performance optimization strategies
- Application monitoring (Elastic APM and Prometheus)

## Documentation (`/docs` folder)

The `/docs` folder serves as living documentation for the project:

**Root Level (`/docs`)**: Core architecture and feature documentation
- Key system overviews and implementation details
- Feature-specific documentation files (e.g., `013-theme-service.md`)

**Subfolders**:
- `planning/` - Design decisions, requirements, and planning documents
- `guidelines/` - Development standards and coding guidelines (e.g., `guidelines-dotnet-consolidated.md`)

**Documentation Strategy**:
- **Living Documentation**: Update docs when making major changes or implementing new features
- **Starting Point**: Check existing docs first when understanding system components
- **Feature Documentation**: Create feature-specific docs for complex implementations
- **Architecture Decisions**: Document significant architectural choices and rationale

When implementing new features or making major changes, update relevant documentation in `/docs` to maintain accurate project knowledge.

## Feature Development Workflow

For feature requests, follow the standardized workflow documented in `docs/feature-development-workflow.md`. This ensures quality through multiple review stages using specialized subagents:

1. **Requirements gathering** (if needed) → requirements-gatherer
2. **Specification creation** → doc-writer → reviewer approval
3. **Implementation** → dotnet-implementation-specialist → reviewer approval  
4. **Finalization** → doc-writer updates docs → git-manager commits

See the full workflow documentation for detailed stages, decision points, and examples.

## Communication Style

Be direct and concise. Avoid filler words like "comprehensive", "extensive", or other AI-speak that adds no value. Get to the point quickly.

## Directory Structure Guide

### Quick Reference for Common Searches

**Components & Views:**
- `Components/Pages/` - Main application pages (Home.razor, etc.)
- `Components/Account/Pages/` - Identity/auth pages (Login.razor, Register.razor, etc.)
- `Components/Account/Pages/Manage/` - User profile management (Index.razor, ChangePassword.razor, etc.)
- `Components/Admin/` - Admin-only pages (Users.razor, Roles.razor, Dashboard.razor)
- `Components/Layout/` - Layout components (MainLayout.razor, TopNavbar.razor, etc.)
- `Components/Shared/` - Reusable components (ThemeSwitcher.razor)

**Services & Business Logic:**
- `Services/` - All application services
  - `NavigationService.cs` - Menu/nav generation
  - `UserRoleService.cs` - Role management
  - `UserManagementService.cs` - User operations
  - `JwtTokenService.cs` - JWT token handling
  - `ThemeService.cs` - Theme switching
  - `*InviteService.cs` - Invitation system
  - `Media/` - File management services
    - `MediaManagementService.cs` - File operations and metadata
    - `FileSecurityService.cs` - Security validation and access control
    - `LocalFileStorageService.cs` - Local storage implementation

**Data & Database:**
- `Data/` - Entity Framework context and models
  - `ApplicationDbContext.cs` - Main EF context
  - `ApplicationUser.cs` - User entity
  - `MediaFile.cs` - File metadata entity
  - `MediaFileAccess.cs` - File access permissions entity
  - `Migrations/` - Database migrations
  - `DataSeeder.cs` - Database seeding

**Configuration:**
- `Configuration/` - Configuration classes
  - `ConfigurationOptions.cs` - Main config model
  - `Navigation/` - Navigation configuration models
- `appsettings.json` - Main configuration file
- `appsettings.Development.json` - Dev overrides

**API & Controllers:**
- `Controllers/` - Web API controllers
  - `UsersController.cs` - User management API
  - `AuthController.cs` - JWT authentication endpoints (login, refresh, profile)
  - `MediaController.cs` - Secure file serving and access control
  - `ApiControllerBase.cs` - Base controller with standardized responses
- `DTO/` - Data transfer objects for API requests and responses
- `Authorization/` - Custom auth policies and API authorization

**Middleware & Extensions:**
- `Middleware/` - Custom middleware (rate limiting, error handling, etc.)
- `Extensions/` - Service registration extensions

**Static Assets:**
- `wwwroot/` - Static files
  - `app.css` - Main styles
  - `color-schemes.css` - Theme styles
  - `js/` - JavaScript files

### Search Tips
- **Authentication issues**: Look in `Components/Account/`, `Services/`, or `Controllers/AuthController.cs`
- **User management**: Check `Services/UserManagementService.cs` or `Controllers/UsersController.cs`
- **Navigation problems**: Start with `Services/NavigationService.cs` or `appsettings.json`
- **Styling/themes**: Check `Services/ThemeService.cs`, `wwwroot/*.css`, or `Components/Shared/ThemeSwitcher.razor`
- **Database issues**: Look in `Data/` folder, especially `ApplicationDbContext.cs`
- **Configuration problems**: Check `Configuration/ConfigurationOptions.cs` or `appsettings.json`
- **File management**: Check `Services/Media/`, `Data/MediaFile.cs`, or `Controllers/MediaController.cs`

## Working Directory
The main project is located in `src/blazor-template/` - most dotnet commands should be run from this directory.

## Major Features Overview

### Invite System
- **Invite Codes**: 8-character alphanumeric codes for easy sharing
- **Email Invites**: Secure token-based email invitations
- **Admin Management**: Full admin interface for creating and managing invites
- **Configurable**: Expiration times, quotas, and system toggle options
- **Documentation**: See `docs/020-invite-system.md`

### JWT API System
- **Authentication Endpoints**: Login, refresh, logout, and profile access
- **Refresh Tokens**: Secure, database-stored refresh tokens with proper lifecycle
- **Rate Limiting**: Configurable protection against abuse
- **Standardized Responses**: Consistent API response patterns
- **Documentation**: See `docs/008-jwt-api-system.md`

### Database Architecture
- **Extended Identity**: Custom ApplicationUser with theme preferences
- **Activity Tracking**: Comprehensive user activity logging
- **Invite Storage**: Dedicated tables for invite codes and email invites
- **Token Management**: Secure refresh token storage and cleanup
- **Documentation**: See `docs/010-database-architecture.md`

### Theme System
- **Server-Side Rendering**: No theme flash, SEO-friendly
- **Persistent Storage**: Database storage with localStorage backup
- **Multiple Themes**: Professional color schemes with dark mode
- **Page Refresh Strategy**: Reliable theme consistency
- **Documentation**: See `docs/013-theme-service.md`

### File Management & Media System
- **Secure File Upload**: Multiple file support with drag-and-drop interface
- **Access Control**: Private, public, and role-based file sharing
- **Storage Management**: Local file storage with organized folder structure
- **File Validation**: MIME type checking, size limits, and security scanning
- **Metadata Management**: Titles, descriptions, tags, and categorization
- **Thumbnail Generation**: Automatic thumbnail creation for image files
- **Deduplication**: SHA-256 hash-based duplicate file detection
- **Documentation**: See `docs/026-file-management-system.md`

## Updates and Memories
- Use single quotes instead of escaping because the compiler doesn't like escaped quotes