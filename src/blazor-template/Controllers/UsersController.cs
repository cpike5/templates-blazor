using BlazorTemplate.DTO;
using BlazorTemplate.DTO.Api;
using BlazorTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Controllers
{
    /// <summary>
    /// User management API endpoints
    /// </summary>
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserManagementService userManagementService,
            ILogger<UsersController> logger)
        {
            _userManagementService = userManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of users with filtering and search
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<PagedApiResponse<UserDto>>> GetUsers([FromQuery] UserSearchCriteria criteria)
        {
            try
            {
                var result = await _userManagementService.GetUsersAsync(criteria);
                return Success(result.Items, criteria.PageNumber, criteria.PageSize, result.TotalCount, 
                    $"Retrieved {result.Items.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users with criteria: {@Criteria}", criteria);
                return StatusCode(500, new PagedApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users",
                    Data = new List<UserDto>(),
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get user details by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUser(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound<UserDetailDto>($"User with ID '{id}' not found");
                }

                return Success(user, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return Error<UserDetailDto>("An error occurred while retrieving user", 500);
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest<UserDto>("Invalid user data", GetModelStateErrors());
            }

            try
            {
                // Check if user already exists
                if (await _userManagementService.UserExistsAsync(request.Email))
                {
                    return Conflict<UserDto>($"User with email '{request.Email}' already exists");
                }

                var result = await _userManagementService.CreateUserAsync(request);
                if (!result.Succeeded)
                {
                    return BadRequest<UserDto>("Failed to create user", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                var createdUser = await _userManagementService.GetUserByEmailAsync(request.Email);
                _logger.LogInformation("User created successfully: {Email} by admin: {AdminId}", 
                    request.Email, GetCurrentUserId());

                return Success(createdUser!, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", request.Email);
                return Error<UserDto>("An error occurred while creating the user", 500);
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest<UserDto>("Invalid user data", GetModelStateErrors());
            }

            try
            {
                var existingUser = await _userManagementService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound<UserDto>($"User with ID '{id}' not found");
                }

                // Check if email is being changed and if new email already exists
                if (!string.Equals(existingUser.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userManagementService.UserExistsAsync(request.Email))
                    {
                        return Conflict<UserDto>($"User with email '{request.Email}' already exists");
                    }
                }

                var result = await _userManagementService.UpdateUserAsync(id, request);
                if (!result.Succeeded)
                {
                    return BadRequest<UserDto>("Failed to update user", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                var updatedUser = await _userManagementService.GetUserByIdAsync(id);
                _logger.LogInformation("User updated successfully: {UserId} by admin: {AdminId}", 
                    id, GetCurrentUserId());

                var userDto = ConvertToUserDto(updatedUser!);
                return Success(userDto, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return Error<UserDto>("An error occurred while updating the user", 500);
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                // Prevent self-deletion
                if (id == GetCurrentUserId())
                {
                    return BadRequest("Cannot delete your own account");
                }

                var result = await _userManagementService.DeleteUserAsync(id);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to delete user", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("User deleted successfully: {UserId} by admin: {AdminId}", 
                    id, GetCurrentUserId());

                return Success("User deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return Error("An error occurred while deleting the user", 500);
            }
        }

        /// <summary>
        /// Lock a user account
        /// </summary>
        [HttpPost("{id}/lock")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> LockUser(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                // Prevent self-locking
                if (id == GetCurrentUserId())
                {
                    return BadRequest("Cannot lock your own account");
                }

                var result = await _userManagementService.LockUserAsync(id);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to lock user", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("User locked successfully: {UserId} by admin: {AdminId}", 
                    id, GetCurrentUserId());

                return Success("User locked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user: {UserId}", id);
                return Error("An error occurred while locking the user", 500);
            }
        }

        /// <summary>
        /// Unlock a user account
        /// </summary>
        [HttpPost("{id}/unlock")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> UnlockUser(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                var result = await _userManagementService.UnlockUserAsync(id);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to unlock user", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("User unlocked successfully: {UserId} by admin: {AdminId}", 
                    id, GetCurrentUserId());

                return Success("User unlocked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user: {UserId}", id);
                return Error("An error occurred while unlocking the user", 500);
            }
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid password reset request", GetModelStateErrors());
            }

            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                var result = await _userManagementService.ResetUserPasswordAsync(id, request.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to reset password", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("Password reset successfully for user: {UserId} by admin: {AdminId}", 
                    id, GetCurrentUserId());

                return Success("Password reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", id);
                return Error("An error occurred while resetting the password", 500);
            }
        }

        /// <summary>
        /// Get user roles
        /// </summary>
        [HttpGet("{id}/roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetUserRoles(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound<List<string>>($"User with ID '{id}' not found");
                }

                var roles = await _userManagementService.GetUserRolesAsync(id);
                return Success(roles, "User roles retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user: {UserId}", id);
                return Error<List<string>>("An error occurred while retrieving user roles", 500);
            }
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        [HttpPost("{id}/roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid role assignment request", GetModelStateErrors());
            }

            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                var result = await _userManagementService.AssignRoleToUserAsync(id, request.RoleName);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to assign role", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("Role '{Role}' assigned to user: {UserId} by admin: {AdminId}", 
                    request.RoleName, id, GetCurrentUserId());

                return Success($"Role '{request.RoleName}' assigned successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role '{Role}' to user: {UserId}", request.RoleName, id);
                return Error("An error occurred while assigning the role", 500);
            }
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        [HttpDelete("{id}/roles/{roleName}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveRole(string id, string roleName)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID '{id}' not found");
                }

                var result = await _userManagementService.RemoveRoleFromUserAsync(id, roleName);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to remove role", 
                        result.Errors.Select(e => e.Description).ToList());
                }

                _logger.LogInformation("Role '{Role}' removed from user: {UserId} by admin: {AdminId}", 
                    roleName, id, GetCurrentUserId());

                return Success($"Role '{roleName}' removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role '{Role}' from user: {UserId}", roleName, id);
                return Error("An error occurred while removing the role", 500);
            }
        }

        /// <summary>
        /// Get user statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetStatistics()
        {
            try
            {
                var stats = await _userManagementService.GetUserStatisticsAsync();
                return Success(stats, "User statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics");
                return Error<UserStatsDto>("An error occurred while retrieving user statistics", 500);
            }
        }

        /// <summary>
        /// Get user activity log
        /// </summary>
        [HttpGet("{id}/activity")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<List<UserActivityDto>>>> GetUserActivity(
            string id, 
            [FromQuery] int count = 10)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound<List<UserActivityDto>>($"User with ID '{id}' not found");
                }

                var activity = await _userManagementService.GetUserActivityAsync(id, count);
                return Success(activity, "User activity retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity for user: {UserId}", id);
                return Error<List<UserActivityDto>>("An error occurred while retrieving user activity", 500);
            }
        }

        /// <summary>
        /// Search users by term
        /// </summary>
        [HttpGet("search")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> SearchUsers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest<List<UserDto>>("Search term is required");
            }

            try
            {
                var users = await _userManagementService.SearchUsersAsync(searchTerm);
                return Success(users, $"Found {users.Count} users matching '{searchTerm}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                return Error<List<UserDto>>("An error occurred while searching users", 500);
            }
        }

        /// <summary>
        /// Convert UserDetailDto to UserDto for API responses
        /// </summary>
        private static UserDto ConvertToUserDto(UserDetailDto userDetail)
        {
            return new UserDto
            {
                Id = userDetail.Id,
                Email = userDetail.Email,
                UserName = userDetail.UserName,
                EmailConfirmed = userDetail.EmailConfirmed,
                LockoutEnabled = userDetail.LockoutEnabled,
                LockoutEnd = userDetail.LockoutEnd,
                CreatedAt = userDetail.CreatedAt,
                LastLogin = userDetail.LastLogin,
                Roles = userDetail.Roles,
                PhoneNumber = userDetail.PhoneNumber,
                TwoFactorEnabled = userDetail.TwoFactorEnabled
            };
        }
    }

    // Additional request models for API endpoints
    public class ResetPasswordRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AssignRoleRequest
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}