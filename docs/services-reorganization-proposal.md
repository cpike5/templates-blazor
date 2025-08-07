# Services Folder Reorganization Proposal

## Current State
The Services folder currently contains 20+ service files in a flat structure, making it difficult to navigate and understand the relationships between services.

## Proposed Structure

```
Services/
├── Auth/
│   ├── IUserManagementService.cs
│   ├── UserManagementService.cs
│   ├── TestUserManagementService.cs
│   ├── IUserRoleService.cs
│   ├── UserRoleService.cs
│   ├── IAdminRoleService.cs
│   └── AdminRoleService.cs
│
├── Invites/
│   ├── IInviteService.cs
│   ├── InviteService.cs
│   ├── IEmailInviteService.cs
│   └── EmailInviteService.cs
│
├── Navigation/
│   ├── INavigationService.cs
│   └── NavigationService.cs
│
├── UI/
│   ├── ThemeService.cs
│   ├── IToastService.cs
│   └── ToastService.cs
│
├── Monitoring/
│   ├── ISystemMonitoringService.cs
│   ├── SystemMonitoringService.cs
│   ├── SystemHealthBackgroundService.cs
│   ├── IDashboardService.cs
│   └── DashboardService.cs
│
├── Settings/
│   ├── ISettingsService.cs
│   ├── SettingsService.cs
│   ├── ISettingsEncryptionService.cs
│   └── SettingsEncryptionService.cs
│
└── Setup/
    ├── IFirstTimeSetupService.cs
    └── FirstTimeSetupService.cs
```

## Benefits

1. **Logical Grouping**: Services are grouped by their primary responsibility
2. **Easier Navigation**: Developers can quickly find related services
3. **Scalability**: New services can be added to appropriate modules
4. **Clear Boundaries**: Module boundaries help prevent circular dependencies
5. **Maintainability**: Related services are co-located for easier updates

## Implementation Steps

### Phase 1: Create Folder Structure
```bash
mkdir Services/Auth
mkdir Services/Invites
mkdir Services/Navigation
mkdir Services/UI
mkdir Services/Monitoring
mkdir Services/Settings
mkdir Services/Setup
```

### Phase 2: Move Files
Move services to their appropriate folders while maintaining git history using `git mv`.

### Phase 3: Update Namespaces
Update namespaces to reflect new structure:
- `BlazorTemplate.Services.Auth`
- `BlazorTemplate.Services.Invites`
- `BlazorTemplate.Services.Navigation`
- `BlazorTemplate.Services.UI`
- `BlazorTemplate.Services.Monitoring`
- `BlazorTemplate.Services.Settings`
- `BlazorTemplate.Services.Setup`

### Phase 4: Update References
Update all `using` statements throughout the codebase to reference the new namespaces.

### Phase 5: Update Service Registration
Update `Program.cs` or extension methods to reflect new namespace organization.

## Alternative Approach: Keep Flat Structure with Naming Convention

If moving files is too disruptive, consider a naming convention approach:
- `Auth.UserManagementService.cs`
- `Auth.UserRoleService.cs`
- `Invites.InviteService.cs`
- `UI.ThemeService.cs`
- `Monitoring.SystemMonitoringService.cs`

This provides visual grouping without changing the folder structure.

## Migration Script

```powershell
# PowerShell script to reorganize services
# Run from src/blazor-template directory

# Create directories
$folders = @("Auth", "Invites", "Navigation", "UI", "Monitoring", "Settings", "Setup")
foreach ($folder in $folders) {
    New-Item -ItemType Directory -Force -Path "Services/$folder"
}

# Move files (using git mv to preserve history)
git mv Services/IUserManagementService.cs Services/Auth/
git mv Services/UserManagementService.cs Services/Auth/
git mv Services/TestUserManagementService.cs Services/Auth/
git mv Services/IUserRoleService.cs Services/Auth/
git mv Services/UserRoleService.cs Services/Auth/
git mv Services/IAdminRoleService.cs Services/Auth/
git mv Services/AdminRoleService.cs Services/Auth/

git mv Services/IInviteService.cs Services/Invites/
git mv Services/InviteService.cs Services/Invites/
git mv Services/IEmailInviteService.cs Services/Invites/
git mv Services/EmailInviteService.cs Services/Invites/

git mv Services/INavigationService.cs Services/Navigation/
git mv Services/NavigationService.cs Services/Navigation/

git mv Services/ThemeService.cs Services/UI/
git mv Services/IToastService.cs Services/UI/
git mv Services/ToastService.cs Services/UI/

git mv Services/ISystemMonitoringService.cs Services/Monitoring/
git mv Services/SystemMonitoringService.cs Services/Monitoring/
git mv Services/SystemHealthBackgroundService.cs Services/Monitoring/
git mv Services/IDashboardService.cs Services/Monitoring/
git mv Services/DashboardService.cs Services/Monitoring/

git mv Services/ISettingsService.cs Services/Settings/
git mv Services/SettingsService.cs Services/Settings/
git mv Services/ISettingsEncryptionService.cs Services/Settings/
git mv Services/SettingsEncryptionService.cs Services/Settings/

git mv Services/IFirstTimeSetupService.cs Services/Setup/
git mv Services/FirstTimeSetupService.cs Services/Setup/
```

## Namespace Update Pattern

Example of updating a service file:
```csharp
// Before
namespace BlazorTemplate.Services

// After
namespace BlazorTemplate.Services.Auth
```

Example of updating references:
```csharp
// Before
using BlazorTemplate.Services;

// After
using BlazorTemplate.Services.Auth;
using BlazorTemplate.Services.UI;
// Add other specific namespaces as needed
```

## Recommendation

I recommend the **folder-based reorganization** as it provides:
- Better long-term maintainability
- Clear module boundaries
- Easier onboarding for new developers
- Natural grouping for future service additions

The migration can be done incrementally, starting with one module at a time to minimize disruption.