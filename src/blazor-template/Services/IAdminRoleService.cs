using BlazorTemplate.Data;
using BlazorTemplate.Models;
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

        // Permission management
        Task<IdentityResult> UpdateRolePermissionsAsync(string roleId, List<string> permissions, string? modifiedBy = null);
        Task<List<string>> GetRolePermissionsByIdAsync(string roleId);
        Task<IdentityResult> AddPermissionToRoleAsync(string roleId, string permission, string? createdBy = null);
        Task<IdentityResult> RemovePermissionFromRoleAsync(string roleId, string permission);
    }
}