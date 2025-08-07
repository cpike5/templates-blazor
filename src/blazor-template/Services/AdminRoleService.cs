using BlazorTemplate.Data;
using BlazorTemplate.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services
{
    public class AdminRoleService : IAdminRoleService
    {
        private readonly ILogger<AdminRoleService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] SystemRoles = { "Administrator", "User" };

        public AdminRoleService(
            ILogger<AdminRoleService> logger,
            IServiceProvider serviceProvider,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var roleDtos = new List<RoleDto>();

            foreach (var role in roles)
            {
                var userCount = await GetRoleUserCountAsync(role.Name!);
                roleDtos.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = GetRoleDescription(role.Name!),
                    IsSystemRole = IsSystemRoleAsync(role.Name!).Result,
                    UserCount = userCount,
                    Permissions = await GetRolePermissionsAsync(role.Name!),
                    CreatedAt = DateTime.UtcNow // This would come from audit table in real implementation
                });
            }

            return roleDtos;
        }

        public async Task<RoleDetailDto> GetRoleByIdAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                throw new ArgumentException("Role not found");

            var userCount = await GetRoleUserCountAsync(role.Name!);
            var assignedUsers = await GetUsersInRoleAsync(role.Name!);

            return new RoleDetailDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = GetRoleDescription(role.Name!),
                IsSystemRole = await IsSystemRoleAsync(role.Name!),
                UserCount = userCount,
                Permissions = await GetRolePermissionsAsync(role.Name!),
                CreatedAt = DateTime.UtcNow, // This would come from audit table
                AssignedUsers = assignedUsers,
                LastModified = DateTime.UtcNow, // This would come from audit table
                CreatedBy = "System", // This would come from audit table
                ModifiedBy = "System" // This would come from audit table
            };
        }

        public async Task<IdentityResult> CreateRoleAsync(CreateRoleRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name is required."
                });
            }

            if (request.Name.Length < 2 || request.Name.Length > 50)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name must be between 2 and 50 characters."
                });
            }

            // Check for invalid characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name contains invalid characters. Only letters, numbers, spaces, hyphens, and underscores are allowed."
                });
            }

            if (await _roleManager.RoleExistsAsync(request.Name))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateRoleName",
                    Description = $"Role '{request.Name}' already exists."
                });
            }

            // Check for system role conflicts
            if (await IsSystemRoleAsync(request.Name))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "SystemRoleConflict",
                    Description = $"Cannot create role '{request.Name}' as it conflicts with a system role."
                });
            }

            var role = new IdentityRole(request.Name);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {RoleName} created successfully by admin", request.Name);
                // Here you would typically store additional metadata like description and permissions
                // in a separate table since IdentityRole doesn't have these fields
            }
            else
            {
                _logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                    request.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<IdentityResult> UpdateRoleAsync(string roleId, UpdateRoleRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name is required."
                });
            }

            if (request.Name.Length < 2 || request.Name.Length > 50)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name must be between 2 and 50 characters."
                });
            }

            // Check for invalid characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRoleName",
                    Description = "Role name contains invalid characters. Only letters, numbers, spaces, hyphens, and underscores are allowed."
                });
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = "Role not found."
                });
            }

            if (await IsSystemRoleAsync(role.Name!))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "SystemRoleProtected",
                    Description = "System roles cannot be modified."
                });
            }

            // Update role name if changed
            if (role.Name != request.Name)
            {
                if (await _roleManager.RoleExistsAsync(request.Name))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DuplicateRoleName",
                        Description = $"Role '{request.Name}' already exists."
                    });
                }

                // Check for system role conflicts
                if (await IsSystemRoleAsync(request.Name))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "SystemRoleConflict",
                        Description = $"Cannot rename role to '{request.Name}' as it conflicts with a system role."
                    });
                }

                var oldName = role.Name;
                role.Name = request.Name;
                role.NormalizedName = request.Name.ToUpperInvariant();
                
                _logger.LogInformation("Role {RoleId} renamed from {OldName} to {NewName}", roleId, oldName, request.Name);
            }

            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {RoleId} updated successfully by admin", roleId);
                // Here you would update additional metadata like description and permissions
            }
            else
            {
                _logger.LogWarning("Failed to update role {RoleId}: {Errors}", 
                    roleId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<IdentityResult> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = "Role not found."
                });
            }

            if (await IsSystemRoleAsync(role.Name!))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "SystemRoleProtected",
                    Description = "System roles cannot be deleted."
                });
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleInUse",
                    Description = $"Cannot delete role '{role.Name}' because it is assigned to {usersInRole.Count} user(s)."
                });
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {RoleName} deleted successfully", role.Name);
            }

            return result;
        }

        public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "User not found."
                });
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = "Role not found."
                });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserAlreadyInRole",
                    Description = $"User is already assigned to role '{roleName}'."
                });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} assigned to role {RoleName}", userId, roleName);
            }

            return result;
        }

        public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "User not found."
                });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotInRole",
                    Description = $"User is not assigned to role '{roleName}'."
                });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} removed from role {RoleName}", userId, roleName);
            }

            return result;
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<List<UserDto>> GetUsersInRoleAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    CreatedAt = DateTime.UtcNow, // This would come from user creation tracking
                    LastLogin = DateTime.MinValue, // This would come from login tracking
                    Roles = userRoles.ToList(),
                    PhoneNumber = user.PhoneNumber,
                    TwoFactorEnabled = user.TwoFactorEnabled
                });
            }

            return userDtos.OrderBy(u => u.Email).ToList();
        }

        public async Task<RoleStatsDto> GetRoleStatisticsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var totalRoles = await _roleManager.Roles.CountAsync();
            var systemRolesCount = SystemRoles.Length;
            var customRolesCount = totalRoles - systemRolesCount;

            // Calculate total assignments
            var totalAssignments = await db.UserRoles.CountAsync();

            return new RoleStatsDto
            {
                TotalRoles = totalRoles,
                SystemRoles = systemRolesCount,
                CustomRoles = customRolesCount,
                TotalAssignments = totalAssignments,
                ActiveRoles = totalRoles, // All roles are considered active in basic implementation
                InactiveRoles = 0
            };
        }

        public async Task<bool> IsSystemRoleAsync(string roleName)
        {
            return await Task.FromResult(SystemRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<List<PermissionDto>> GetAvailablePermissionsAsync()
        {
            // This would typically come from a database or configuration
            // For now, return a static list of common permissions
            return await Task.FromResult(new List<PermissionDto>
            {
                // User Management
                new() { Name = "users.view", DisplayName = "View Users", Description = "Access user list and profiles", Category = "User Management", Icon = "fas fa-users" },
                new() { Name = "users.create", DisplayName = "Create Users", Description = "Add new user accounts", Category = "User Management", Icon = "fas fa-user-plus" },
                new() { Name = "users.edit", DisplayName = "Edit Users", Description = "Modify user information", Category = "User Management", Icon = "fas fa-user-edit" },
                new() { Name = "users.delete", DisplayName = "Delete Users", Description = "Remove user accounts", Category = "User Management", Icon = "fas fa-user-minus" },

                // Role Management
                new() { Name = "roles.view", DisplayName = "View Roles", Description = "Access role list and details", Category = "Role Management", Icon = "fas fa-user-shield" },
                new() { Name = "roles.manage", DisplayName = "Manage Roles", Description = "Create and edit roles", Category = "Role Management", Icon = "fas fa-user-cog" },
                new() { Name = "roles.assign", DisplayName = "Assign Roles", Description = "Assign roles to users", Category = "Role Management", Icon = "fas fa-user-tag" },

                // System Administration
                new() { Name = "system.settings", DisplayName = "System Settings", Description = "Access system configuration", Category = "System Admin", Icon = "fas fa-cog" },
                new() { Name = "system.logs", DisplayName = "View Logs", Description = "Access system and audit logs", Category = "System Admin", Icon = "fas fa-file-alt" },
                new() { Name = "system.backup", DisplayName = "Backup Data", Description = "Create and manage backups", Category = "System Admin", Icon = "fas fa-download" },

                // Content Management
                new() { Name = "content.view", DisplayName = "View Content", Description = "Read access to content", Category = "Content", Icon = "fas fa-eye" },
                new() { Name = "content.create", DisplayName = "Create Content", Description = "Add new content items", Category = "Content", Icon = "fas fa-plus" },
                new() { Name = "content.edit", DisplayName = "Edit Content", Description = "Modify existing content", Category = "Content", Icon = "fas fa-edit" },
                new() { Name = "content.publish", DisplayName = "Publish Content", Description = "Make content publicly visible", Category = "Content", Icon = "fas fa-upload" }
            });
        }

        // Private helper methods
        private async Task<int> GetRoleUserCountAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.Count;
        }

        private async Task<List<string>> GetRolePermissionsAsync(string roleName)
        {
            // This would typically come from a database table that stores role-permission mappings
            // For now, return predefined permissions based on role type
            return await Task.FromResult(roleName.ToLowerInvariant() switch
            {
                "administrator" => new List<string> { "All Permissions" },
                "user" => new List<string> { "content.view", "users.view" },
                "manager" => new List<string> { "content.view", "content.edit", "users.view", "users.edit" },
                "viewer" => new List<string> { "content.view" },
                _ => new List<string>()
            });
        }

        private static string GetRoleDescription(string roleName)
        {
            return roleName.ToLowerInvariant() switch
            {
                "administrator" => "Full system access and control",
                "user" => "Standard user access",
                "manager" => "Team management capabilities",
                "viewer" => "Read-only access to specific areas",
                _ => "Custom role with specific permissions"
            };
        }
    }
}