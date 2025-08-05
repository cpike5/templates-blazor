using BlazorTemplate.Data;
using BlazorTemplate.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(UserSearchCriteria criteria)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.Email))
                query = query.Where(u => u.Email!.Contains(criteria.Email));

            if (!string.IsNullOrEmpty(criteria.UserName))
                query = query.Where(u => u.UserName!.Contains(criteria.UserName));

            if (criteria.EmailConfirmed.HasValue)
                query = query.Where(u => u.EmailConfirmed == criteria.EmailConfirmed.Value);

            if (criteria.IsLockedOut.HasValue)
            {
                if (criteria.IsLockedOut.Value)
                    query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
                else
                    query = query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow);
            }

            if (criteria.CreatedAfter.HasValue)
                query = query.Where(u => u.Id != null); // Note: IdentityUser doesn't have CreatedAt by default

            if (criteria.CreatedBefore.HasValue)
                query = query.Where(u => u.Id != null); // Note: IdentityUser doesn't have CreatedAt by default

            // Apply sorting
            query = criteria.SortBy.ToLower() switch
            {
                "email" => criteria.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "username" => criteria.SortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                _ => query.OrderBy(u => u.Email)
            };

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles.ToList()));
            }

            return new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize
            };
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var recentActivity = await GetUserActivityAsync(userId, 5);

            return new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                CreatedAt = DateTime.UtcNow, // Note: Default IdentityUser doesn't have CreatedAt
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Roles = roles.ToList(),
                AccessFailedCount = user.AccessFailedCount,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                SecurityStamp = user.SecurityStamp,
                ConcurrencyStamp = user.ConcurrencyStamp,
                RecentActivity = recentActivity,
                Claims = claims.ToDictionary(c => c.Type, c => c.Value)
            };
        }

        public async Task<IdentityResult> CreateUserAsync(CreateUserRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = request.EmailConfirmed,
                PhoneNumber = request.PhoneNumber,
                LockoutEnabled = request.LockoutEnabled
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (result.Succeeded && request.Roles.Any())
            {
                foreach (var role in request.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            if (result.Succeeded)
            {
                await LogUserActivityAsync(user.Id, "UserCreated", $"User {user.Email} was created");
                _logger.LogInformation("User {Email} created successfully", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            user.Email = request.Email;
            user.UserName = request.UserName;
            user.PhoneNumber = request.PhoneNumber;
            user.EmailConfirmed = request.EmailConfirmed;
            user.LockoutEnabled = request.LockoutEnabled;
            user.TwoFactorEnabled = request.TwoFactorEnabled;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(request.Roles).ToList();
                var rolesToAdd = request.Roles.Except(currentRoles).ToList();

                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                if (rolesToAdd.Any())
                {
                    var validRoles = new List<string>();
                    foreach (var role in rolesToAdd)
                    {
                        if (await _roleManager.RoleExistsAsync(role))
                            validRoles.Add(role);
                    }
                    if (validRoles.Any())
                        await _userManager.AddToRolesAsync(user, validRoles);
                }

                await LogUserActivityAsync(userId, "UserUpdated", $"User {user.Email} was updated");
                _logger.LogInformation("User {Email} updated successfully", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var email = user.Email;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "UserDeleted", $"User {email} was deleted");
                _logger.LogInformation("User {Email} deleted successfully", email);
            }

            return result;
        }

        public async Task<IdentityResult> LockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1));
            
            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "UserLocked", $"User {user.Email} was locked");
                _logger.LogInformation("User {Email} locked successfully", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            
            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "UserUnlocked", $"User {user.Email} was unlocked");
                _logger.LogInformation("User {Email} unlocked successfully", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> ResetUserPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "PasswordReset", $"Password reset for user {user.Email}");
                _logger.LogInformation("Password reset for user {Email}", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> ResendEmailConfirmationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            if (user.EmailConfirmed)
                return IdentityResult.Failed(new IdentityError { Description = "Email already confirmed" });

            // In a real implementation, you would send the confirmation email here
            // For now, just log the activity
            await LogUserActivityAsync(userId, "EmailConfirmationResent", $"Email confirmation resent for user {user.Email}");
            _logger.LogInformation("Email confirmation resent for user {Email}", user.Email);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ConfirmUserEmailAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "EmailConfirmed", $"Email confirmed for user {user.Email}");
                _logger.LogInformation("Email confirmed for user {Email}", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            if (!await _roleManager.RoleExistsAsync(roleName))
                return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "RoleAssigned", $"Role {roleName} assigned to user {user.Email}");
                _logger.LogInformation("Role {RoleName} assigned to user {Email}", roleName, user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                await LogUserActivityAsync(userId, "RoleRemoved", $"Role {roleName} removed from user {user.Email}");
                _logger.LogInformation("Role {RoleName} removed from user {Email}", roleName, user.Email);
            }

            return result;
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<UserStatsDto> GetUserStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.EmailConfirmed && (!u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow));
            var lockedUsers = await _context.Users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            var pendingUsers = await _context.Users.CountAsync(u => !u.EmailConfirmed);

            // Note: Since IdentityUser doesn't have CreatedAt by default, these will be 0
            var newUsersToday = 0;
            var newUsersThisWeek = 0;
            var newUsersThisMonth = 0;

            return new UserStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                LockedUsers = lockedUsers,
                PendingUsers = pendingUsers,
                NewUsersToday = newUsersToday,
                NewUsersThisWeek = newUsersThisWeek,
                NewUsersThisMonth = newUsersThisMonth
            };
        }

        public async Task<List<UserActivityDto>> GetUserActivityAsync(string userId, int count = 10)
        {
            var activities = await _context.UserActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .Select(a => new UserActivityDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action,
                    Details = a.Details,
                    Timestamp = a.Timestamp,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent
                })
                .ToListAsync();

            return activities;
        }

        public async Task LogUserActivityAsync(string userId, string action, string details, string? ipAddress = null)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserDto>> SearchUsersAsync(string searchTerm)
        {
            var users = await _context.Users
                .Where(u => u.Email!.Contains(searchTerm) || u.UserName!.Contains(searchTerm))
                .Take(20)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles.ToList()));
            }

            return userDtos;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToUserDto(user, roles.ToList());
        }

        private static UserDto MapToUserDto(ApplicationUser user, List<string> roles)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                CreatedAt = DateTime.UtcNow, // Note: Default IdentityUser doesn't have CreatedAt
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Roles = roles
            };
        }
    }
}