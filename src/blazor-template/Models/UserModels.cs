using BlazorTemplate.Data;

namespace BlazorTemplate.Models
{
    /// <summary>
    /// Simple user data transfer object for UI display
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public string Theme { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        public string DisplayName => !string.IsNullOrEmpty(UserName) ? UserName : Email;
        public string Initials => GetInitials(DisplayName);
        public string Status => GetStatus();
        
        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "?";

            var parts = name.Split('@')[0].Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
        
        private string GetStatus()
        {
            if (LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow)
                return "Locked";
            if (!EmailConfirmed)
                return "Pending";
            return "Active";
        }
    }

    /// <summary>
    /// Detailed user information for admin views
    /// </summary>
    public class UserDetailDto : UserDto
    {
        public new string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneNumberConfirmed { get; set; }
        public new bool TwoFactorEnabled { get; set; }
        public List<UserActivityDto> RecentActivity { get; set; } = new();
        public string SecurityStamp { get; set; } = string.Empty;
        public string ConcurrencyStamp { get; set; } = string.Empty;
        public List<string> Claims { get; set; } = new();
    }

    /// <summary>
    /// User activity information
    /// </summary>
    public class UserActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        public static UserActivityDto FromEntity(UserActivity activity)
        {
            return new UserActivityDto
            {
                Id = activity.Id,
                UserId = activity.UserId ?? string.Empty,
                Action = activity.Action ?? string.Empty,
                Details = activity.Details ?? string.Empty,
                Timestamp = activity.Timestamp,
                IpAddress = activity.IpAddress,
                UserAgent = activity.UserAgent // UserActivity does have UserAgent field
            };
        }
    }

    /// <summary>
    /// User statistics summary
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int UnconfirmedUsers { get; set; }
        public int PendingUsers { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewUsersToday { get; set; }
    }

    /// <summary>
    /// Generic paged result wrapper
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasPreviousPage => CurrentPage > 1;
    }

    /// <summary>
    /// User search criteria
    /// </summary>
    public class UserSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? IsLockedOut { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "Email";
        public bool SortDescending { get; set; } = false;
    }

    /// <summary>
    /// Request model for creating new users
    /// </summary>
    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = false;
        public bool LockoutEnabled { get; set; } = true;
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// Request model for updating existing users
    /// </summary>
    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// Role data transfer object
    /// </summary>
    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public int UserCount { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Detailed role information
    /// </summary>
    public class RoleDetailDto : RoleDto
    {
        public List<UserDto> AssignedUsers { get; set; } = new();
        public DateTime? LastModified { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// Request model for creating new roles
    /// </summary>
    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request model for updating existing roles
    /// </summary>
    public class UpdateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Role statistics summary
    /// </summary>
    public class RoleStatsDto
    {
        public int TotalRoles { get; set; }
        public int SystemRoles { get; set; }
        public int CustomRoles { get; set; }
        public int TotalAssignments { get; set; }
        public int ActiveRoles { get; set; }
        public int InactiveRoles { get; set; }
    }

    /// <summary>
    /// Permission information
    /// </summary>
    public class PermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-key";
    }
}