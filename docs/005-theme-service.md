# Theme Service Architecture

## Overview

The theme system provides server-side rendered dynamic visual theming with persistent user preferences. Built on Blazor Server with server-first rendering and page refresh strategy to eliminate theme flashing and caching issues.

## Architecture Philosophy

**Server-Side First**: Themes are detected and applied during initial server rendering, ensuring no flash of unstyled content or theme switching on page load.

**Page Refresh Strategy**: Theme changes trigger page refreshes to ensure consistent server-side theme application across all pages.

## Core Components

### ThemeService (`Services/ThemeService.cs`)
Central service managing theme state, persistence, and application.

**Dependencies:**
- `IJSRuntime` - localStorage backup persistence
- `AuthenticationStateProvider` - User authentication state  
- `UserManager<ApplicationUser>` - User management
- `ApplicationDbContext` - Primary theme persistence
- `NavigationManager` - Page refresh coordination

**Key Methods:**
- `EnsureInitializedAsync()` - Thread-safe server-side theme initialization
- `InitializeAsync(afterRender: true)` - Client-side localStorage synchronization
- `SetThemeAsync(theme)` - Changes theme with persistence and page refresh
- `ToggleDarkModeAsync()` - Toggles dark mode with persistence and page refresh
- `GetAvailableThemes()` - Returns available theme configurations

**State Management:**
- `CurrentTheme` - Current theme identifier (read-only)
- `IsDarkMode` - Dark mode toggle state (read-only)
- `OnThemeChanged` - Event for component state updates (rarely needed)

**Initialization Flow:**
```
EnsureInitializedAsync() (server-side)
├── Load from database (authenticated users)  
├── Set defaults (anonymous/no preference)
└── Thread-safe single initialization

InitializeAsync(afterRender: true) (client-side)
├── Sync localStorage with database preferences
└── Ensure localStorage consistency
```

### App.razor Integration (`Components/App.razor`)
Root component applying theme classes to HTML element during server rendering.

```razor
@using BlazorTemplate.Services
@inject ThemeService ThemeService

<!DOCTYPE html>
<html lang="en" class="@ThemeClasses">
```

**Theme Class Generation:**
```csharp
protected override async Task OnInitializedAsync()
{
    // Initialize theme service server-side
    await ThemeService.EnsureInitializedAsync();
    ThemeClasses = GetThemeClasses(); // Builds class string from theme state
}

private string GetThemeClasses()
{
    var classes = new List<string>();
    
    // Add config-based color scheme
    if (!string.IsNullOrEmpty(ColorScheme))
        classes.Add(ColorScheme);
    
    // Add theme classes from ThemeService
    if (ThemeService.CurrentTheme != "default")
        classes.Add(ThemeService.CurrentTheme);
    
    if (ThemeService.IsDarkMode)
        classes.Add("dark-mode");
    
    return string.Join(" ", classes);
}
```

### ThemeSwitcher (`Components/Shared/ThemeSwitcher.razor`)
UI component providing theme selection interface.

**Features:**
- Theme dropdown with visual previews
- Dark mode toggle
- Automatic page refresh on changes
- Server-side theme state integration

### JavaScript Support (`wwwroot/js/theme.js`)
Simplified client-side theme utilities (backup only).

```javascript
// Simple theme manager for fallback scenarios
window.themeManager = {
    setRootTheme: (themeClass) => {
        // Direct DOM class manipulation
        // Used as fallback if server-side fails
    }
}
```

**Note:** JavaScript is no longer responsible for initial theme loading or primary theme switching.

## Data Persistence Strategy

### Primary: Database Storage
**Table:** `AspNetUsers`  
**Field:** `ThemePreference` (nullable string)  
**Format:** `theme|isDarkMode` (e.g., `trust-blue|true`)

**For authenticated users:**
1. Theme preferences stored in database
2. Server-side theme detection on each request
3. localStorage synchronized for consistency
4. Database is authoritative source

### Backup: localStorage
**Keys:**
- `app-theme` - Theme identifier
- `app-dark-mode` - Dark mode state (boolean string)

**For anonymous users:**
1. Primary storage in localStorage
2. Server attempts localStorage read during SSR
3. Maintains theme across browser sessions
4. No database interaction

## Theme Loading Priority & Flow

### Server-Side Initialization (Every Page Load)
1. **Authenticated Users:**
   ```
   Database ThemePreference → Parse theme + dark mode → Apply to HTML
   ```

2. **Anonymous Users:**
   ```
   Default theme → (Client-side sync with localStorage later)
   ```

### Client-Side Synchronization (After Render)
1. **Read localStorage values**
2. **Compare with server-side state**  
3. **Update localStorage if database preferences exist**
4. **Keep localStorage for anonymous users**

### Theme Change Flow
```
User clicks theme → ThemeService.SetThemeAsync()
├── Update internal state
├── Save to database (if authenticated)  
├── Save to localStorage (backup)
└── NavigationManager.Refresh() → New page with correct theme
```

## Available Themes

Pre-configured themes with CSS class mappings:

| Theme ID | Display Name | Primary Color |
|----------|-------------|---------------|
| `default` | Professional Neutral | #475569 |
| `executive-purple` | Executive Purple | #782789 |
| `sunset-rose` | Sunset Rose | #ec4899 |
| `trust-blue` | Trust Blue | #1e40af |
| `growth-green` | Growth Green | #16a34a |
| `innovation-orange` | Innovation Orange | #ea580c |
| `midnight-teal` | Midnight Teal | #0f766e |
| `heritage-burgundy` | Heritage Burgundy | #be123c |
| `platinum-gray` | Platinum Gray | #374151 |
| `deep-navy` | Deep Navy | #1e3a8a |

Each theme can be combined with `dark-mode` class for dark variants.

## Integration Points

### Service Registration (`Program.cs`)
```csharp
builder.Services.AddScoped<ThemeService>();
```

### CSS Theme System (`wwwroot/color-schemes.css`)
Theme-specific CSS variables and styling rules applied via HTML classes.

## Critical Implementation Details

### Server-Side Rendering Benefits
- **No Theme Flash**: Correct theme applied in initial HTML response
- **SEO Friendly**: Themes visible to crawlers and disabled-JS users
- **Performance**: No client-side theme switching delays
- **Consistency**: Same theme rendering across all page loads

### Page Refresh Strategy
- **Reliability**: Guarantees theme consistency across components
- **Simplicity**: Eliminates complex state synchronization
- **Performance**: Modern browsers handle refreshes efficiently
- **User Experience**: Brief refresh preferable to theme flickering

### Thread Safety
- **SemaphoreSlim**: Prevents race conditions during initialization
- **Single Initialization**: `EnsureInitializedAsync()` called once per service lifetime
- **State Consistency**: All theme reads happen after initialization

### Error Handling Patterns
```csharp
try
{
    // Database operations
    await _userManager.UpdateAsync(user);
}
catch
{
    // Always attempt page refresh even if persistence fails
    _navigationManager.Refresh();
}
```

## Breaking Changes from Previous Architecture

### Removed Components
- `ThemeApplicator.razor` - No longer needed with server-side approach
- Complex JavaScript initialization - Simplified to basic utilities

### Changed Behavior  
- **Theme switching now refreshes page** (was JavaScript-based)
- **Themes load immediately with page** (no flash or delay)
- **localStorage is backup only** (was primary for anonymous users)

### Migration Notes
- Existing user theme preferences preserved
- localStorage themes automatically migrated on first load
- No action required for existing installations

## Troubleshooting

### Theme Not Persisting
1. Check database `ThemePreference` field for authenticated users
2. Verify localStorage for anonymous users  
3. Ensure `ThemeService` is registered in DI container

### Theme Flash on Load
- Should not occur with server-side architecture
- If present, verify `App.razor` properly injects and initializes `ThemeService`

### Theme Not Switching
1. Confirm page refresh occurs after theme change
2. Check browser console for JavaScript errors
3. Verify database/localStorage updates

### Performance Concerns
- Page refreshes are intentional and necessary
- Modern browsers optimize refresh performance
- Alternative: Complex state synchronization (not recommended)

## Future Enhancements

- System theme detection (prefers-color-scheme)
- Custom theme creation interface  
- Theme preview without applying
- Animated transitions (within single page)
- Theme scheduling (time-based switching)
- Organization-wide theme policies