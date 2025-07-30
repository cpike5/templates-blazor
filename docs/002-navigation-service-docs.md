# NavigationService Documentation

The NavigationService manages the application's navigation menu system with role-based filtering.

## Interface

```csharp
public interface INavigationService
{
    Task<List<NavigationItem>> GetNavigationItemsAsync();
    Task<List<NavigationItem>> GetNavigationItemsForUserAsync(IEnumerable<string> userRoles);
    Task<NavigationItem?> FindNavigationItemAsync(string id);
    bool IsItemAccessible(NavigationItem item, IEnumerable<string> userRoles);
}
```

## Configuration

Navigation items are configured in `appsettings.json`:

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

## NavigationItem Properties

- `Id`: Unique identifier for the navigation item
- `Title`: Display text for the menu item
- `Href`: URL path (optional for group items)
- `Icon`: Font Awesome icon class
- `RequiredRoles`: Array of roles required to see this item (empty = visible to all)
- `Order`: Sort order for menu items
- `Children`: Sub-menu items for grouped navigation
- `IsVisible`: Whether the item should be displayed (default: true)

## Role Filtering

- Empty `RequiredRoles` array means the item is visible to everyone
- Items with role requirements are only shown to users with matching roles
- Parent groups without accessible children are hidden
- Child items are filtered recursively

## Usage in Components

The service is used in `SidebarNavigation.razor`:

```csharp
@inject INavigationService NavigationService
@inject AuthenticationStateProvider AuthenticationStateProvider

protected override async Task OnInitializedAsync()
{
    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    var userRoles = authState.User.Claims
        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList();

    // If user is not authenticated, add "Anonymous" role
    if (!authState.User.Identity?.IsAuthenticated ?? true)
    {
        userRoles.Add("Anonymous");
    }

    navigationItems = await NavigationService.GetNavigationItemsForUserAsync(userRoles);
}
```

## Registration

The service is registered in `Program.cs` via the extension method:

```csharp
builder.Services.AddNavigationServices(builder.Configuration);
```