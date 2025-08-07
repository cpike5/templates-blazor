using BlazorTemplate.Models;
using Microsoft.AspNetCore.Identity;

namespace BlazorTemplate.Services
{
    public interface IUserManagementService
    {
        // User CRUD operations
        Task<PagedResult<UserDto>> GetUsersAsync(UserSearchCriteria criteria);
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        Task<IdentityResult> CreateUserAsync(CreateUserRequest request);
        Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request);
        Task<IdentityResult> DeleteUserAsync(string userId);

        // User account management
        Task<IdentityResult> LockUserAsync(string userId);
        Task<IdentityResult> UnlockUserAsync(string userId);
        Task<IdentityResult> ResetUserPasswordAsync(string userId, string newPassword);
        Task<IdentityResult> ResendEmailConfirmationAsync(string userId);
        Task<IdentityResult> ConfirmUserEmailAsync(string userId);

        // User role management
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);

        // User statistics and activity
        Task<UserStatsDto> GetUserStatisticsAsync();
        Task<List<UserActivityDto>> GetUserActivityAsync(string userId, int count = 10);
        Task LogUserActivityAsync(string userId, string action, string details, string? ipAddress = null);

        // User search and filtering
        Task<List<UserDto>> SearchUsersAsync(string searchTerm);
        Task<bool> UserExistsAsync(string email);
        Task<UserDto?> GetUserByEmailAsync(string email);
    }
}