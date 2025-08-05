using BlazorTemplate.Data;
using BlazorTemplate.DTO;
using Microsoft.AspNetCore.Identity;

namespace BlazorTemplate.Services
{
    public interface IAdminRoleService
    {
        // Role CRUD operations
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDetailDto> GetRoleByIdAsync(string roleId);
        Task<IdentityResult> CreateRoleAsync(CreateRoleRequest request);
        Task<IdentityResult> UpdateRoleAsync(string roleId, UpdateRoleRequest request);
        Task<IdentityResult> DeleteRoleAsync(string roleId);

        // Role assignments
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<List<UserDto>> GetUsersInRoleAsync(string roleName);

        // Role statistics and metadata
        Task<RoleStatsDto> GetRoleStatisticsAsync();
        Task<bool> IsSystemRoleAsync(string roleName);
        Task<List<PermissionDto>> GetAvailablePermissionsAsync();
    }

    // Data Transfer Objects
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

    public class RoleDetailDto : RoleDto
    {
        public List<UserDto> AssignedUsers { get; set; } = new();
        public DateTime? LastModified { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
    }

    // Using existing UserDto from BlazorTemplate.DTO

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class RoleStatsDto
    {
        public int TotalRoles { get; set; }
        public int SystemRoles { get; set; }
        public int CustomRoles { get; set; }
        public int TotalAssignments { get; set; }
        public int ActiveRoles { get; set; }
        public int InactiveRoles { get; set; }
    }

    public class PermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-key";
    }
}