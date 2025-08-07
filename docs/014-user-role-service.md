# UserRoleService Documentation

The UserRoleService manages user role assignments and provides user management functionality.

## Interface

```csharp
public interface IUserRoleService
{
    Task<IEnumerable<ApplicationUser>> GetUsersAsync();
    Task<IEnumerable<IdentityUserRole<string>>> GetUserRolesAsync(string email);
    Task AddUserToRoleAsync(string email, string roleName);
}
```

## Methods

### GetUsersAsync()
- Returns all users in the system
- Used for user management interfaces

### GetUserRolesAsync(string email)
- Returns all role assignments for a specific user
- Takes user email as parameter

### AddUserToRoleAsync(string email, string roleName)
- Assigns a role to a user
- Creates the assignment if it doesn't exist
- Logs the action for auditing

## Implementation Notes

The service includes a `RemoveUserFromRoleAsync` method that's not exposed in the interface but available in the implementation:

```csharp
public async Task RemoveUserFromRoleAsync(string email, string roleName)
```

## Role Management

Application roles are defined in `Program.cs` in the `GetApplicationRoles()` method:

```csharp
private static IEnumerable<string> GetApplicationRoles()
{
    var roles = new List<string>()
    {
        "Administrator",
        "User"
    };
    return roles;
}
```

Roles are automatically seeded into the database on application startup.

## Admin User Setup

The admin user is automatically assigned the Administrator role based on the email configured in `appsettings.json`:

```json
{
  "Site": {
    "AdminEmail": "admin@example.com"
  }
}
```

During database seeding in `Program.cs`, the system:
1. Creates missing roles from `GetApplicationRoles()`
2. Finds the user with the admin email
3. Assigns the Administrator role if not already assigned

## Registration

The service is registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
```

## Dependencies

- `ILogger<UserRoleService>` - For logging role changes
- `ApplicationDbContext` - For database operations

## Database Operations

The service works directly with Entity Framework entities:
- `ApplicationUser` from `AspNetUsers` table
- `IdentityRole` from `AspNetRoles` table  
- `IdentityUserRole<string>` from `AspNetUserRoles` table

All operations call `SaveChangesAsync()` to persist changes to the database.