using BlazorTemplate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services
{
    public interface IUserRoleService
    {
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        Task<IEnumerable<string>> GetUserRolesAsync(string? email);
        Task AddUserToRoleAsync(string email, string roleName);
        Task RemoveUserFromRoleAsync(string email, string roleName);
    }

    public class UserRoleService : IUserRoleService
    {
        private readonly ILogger<UserRoleService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public UserRoleService(ILogger<UserRoleService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var users = await db.Users.ToListAsync();
            return users;
        }

        public async Task AddUserToRoleAsync(string email, string roleName)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

            if (user != null && role != null && !await db.UserRoles.AnyAsync(usr => usr.UserId == user.Id && usr.RoleId == role.Id))
            {
                _logger.LogDebug("Adding user {Email} to role {RoleName}", email, roleName);
                db.UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = role.Id,
                    UserId = user.Id
                });
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveUserFromRoleAsync(string email, string roleName)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new ArgumentException("Invalid user");

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
                throw new ArgumentException("Invalid role");

            var userRole = await db.UserRoles.FirstOrDefaultAsync(userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id);
            if (userRole == null)
                throw new ArgumentException("User not assigned to role");

            _logger.LogDebug("Removing user {Email} from role {RoleName}", email, roleName);
            db.UserRoles.Remove(userRole);
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string? email)
        {
            if (string.IsNullOrEmpty(email))
                return new List<string>();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return new List<string>();

                var roles = await db.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToListAsync();

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving roles for user {Email}", email);
                return new List<string>();
            }
        }
    }
}