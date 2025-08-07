using BlazorTemplate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BlazorTemplate.Services.Auth
{
    /// <summary>
    /// Service interface for managing user role assignments and queries
    /// </summary>
    public interface IUserRoleService
    {
        /// <summary>
        /// Retrieves all users from the system
        /// </summary>
        /// <returns>Collection of application users</returns>
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        
        /// <summary>
        /// Gets all roles assigned to a specific user
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <returns>Collection of role names assigned to the user</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(string? email);
        
        /// <summary>
        /// Adds a user to a specific role
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <param name='roleName'>Name of the role to assign</param>
        Task AddUserToRoleAsync(string email, string roleName);
        
        /// <summary>
        /// Removes a user from a specific role
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <param name='roleName'>Name of the role to remove</param>
        Task RemoveUserFromRoleAsync(string email, string roleName);
    }

    /// <summary>
    /// Service implementation for managing user role assignments and queries
    /// </summary>
    public class UserRoleService : IUserRoleService
    {
        private readonly ILogger<UserRoleService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public UserRoleService(ILogger<UserRoleService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            _logger.LogDebug("UserRoleService initialized");
        }

        /// <summary>
        /// Retrieves all users from the database
        /// </summary>
        /// <returns>Collection of all application users</returns>
        /// <exception cref='InvalidOperationException'>Thrown when user retrieval fails</exception>
        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogTrace("Starting user retrieval operation");
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                _logger.LogTrace("Database context acquired, executing user query");

                var users = await db.Users.ToListAsync();
                
                stopwatch.Stop();
                _logger.LogInformation("Retrieved {UserCount} users from database in {ElapsedMs}ms",
                    users.Count, stopwatch.ElapsedMilliseconds);
                
                return users;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve users from database after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Failed to retrieve users from database", ex);
            }
        }

        /// <summary>
        /// Adds a user to a specific role if not already assigned
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <param name='roleName'>Name of the role to assign</param>
        /// <exception cref='ArgumentException'>Thrown when user or role is not found</exception>
        /// <exception cref='InvalidOperationException'>Thrown when role assignment fails</exception>
        public async Task AddUserToRoleAsync(string email, string roleName)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }
            
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));
            }
            
            _logger.LogInformation("Adding user {Email} to role {RoleName}", email, roleName);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                _logger.LogTrace("Looking up user and role in database");

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found", email);
                    throw new ArgumentException($"User with email {email} not found", nameof(email));
                }

                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleName} not found", roleName);
                    throw new ArgumentException($"Role {roleName} not found", nameof(roleName));
                }
                
                // Check if user is already in role
                var existingAssignment = await db.UserRoles.AnyAsync(usr => usr.UserId == user.Id && usr.RoleId == role.Id);
                
                if (existingAssignment)
                {
                    _logger.LogInformation("User {Email} is already assigned to role {RoleName}, skipping assignment",
                        email, roleName);
                    return;
                }
                
                _logger.LogDebug("Creating role assignment: User {UserId} ({Email}) -> Role {RoleId} ({RoleName})",
                    user.Id, email, role.Id, roleName);
                    
                db.UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = role.Id,
                    UserId = user.Id
                });
                
                await db.SaveChangesAsync();
                
                stopwatch.Stop();
                _logger.LogInformation("Successfully added user {Email} to role {RoleName} in {ElapsedMs}ms",
                    email, roleName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to add user {Email} to role {RoleName} after {ElapsedMs}ms",
                    email, roleName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to add user {email} to role {roleName}", ex);
            }
        }

        /// <summary>
        /// Removes a user from a specific role
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <param name='roleName'>Name of the role to remove</param>
        /// <exception cref='ArgumentException'>Thrown when user, role not found, or user not assigned to role</exception>
        /// <exception cref='InvalidOperationException'>Thrown when role removal fails</exception>
        public async Task RemoveUserFromRoleAsync(string email, string roleName)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }
            
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));
            }
            
            _logger.LogInformation("Removing user {Email} from role {RoleName}", email, roleName);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                _logger.LogTrace("Looking up user and role in database");

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found", email);
                    throw new ArgumentException($"User with email {email} not found", nameof(email));
                }

                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleName} not found", roleName);
                    throw new ArgumentException($"Role {roleName} not found", nameof(roleName));
                }

                var userRole = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                if (userRole == null)
                {
                    _logger.LogWarning("User {Email} is not assigned to role {RoleName}", email, roleName);
                    throw new ArgumentException($"User {email} is not assigned to role {roleName}");
                }
                
                _logger.LogDebug("Removing role assignment: User {UserId} ({Email}) from Role {RoleId} ({RoleName})",
                    user.Id, email, role.Id, roleName);

                db.UserRoles.Remove(userRole);
                await db.SaveChangesAsync();
                
                stopwatch.Stop();
                _logger.LogInformation("Successfully removed user {Email} from role {RoleName} in {ElapsedMs}ms",
                    email, roleName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to remove user {Email} from role {RoleName} after {ElapsedMs}ms",
                    email, roleName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to remove user {email} from role {roleName}", ex);
            }
        }

        /// <summary>
        /// Retrieves all roles assigned to a specific user
        /// </summary>
        /// <param name='email'>User email address</param>
        /// <returns>Collection of role names assigned to the user</returns>
        public async Task<IEnumerable<string>> GetUserRolesAsync(string? email)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogTrace("GetUserRolesAsync called with null or empty email, returning empty list");
                return new List<string>();
            }
            
            _logger.LogTrace("Retrieving roles for user {Email}", email);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                _logger.LogTrace("Looking up user in database");

                var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogInformation("User with email {Email} not found, returning empty roles list", email);
                    stopwatch.Stop();
                    return new List<string>();
                }
                
                _logger.LogTrace("User found: {UserId}, querying role assignments", user.Id);

                var roles = await db.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToListAsync();
                
                stopwatch.Stop();
                _logger.LogInformation("Retrieved {RoleCount} roles for user {Email} in {ElapsedMs}ms: {Roles}",
                    roles.Count, email, stopwatch.ElapsedMilliseconds, string.Join(", ", roles));

                return roles;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Error retrieving roles for user {Email} after {ElapsedMs}ms",
                    email, stopwatch.ElapsedMilliseconds);
                return new List<string>();
            }
        }
    }
}