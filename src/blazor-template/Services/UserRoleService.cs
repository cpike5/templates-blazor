using BlazorTemplate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services
{
    public interface IUserRoleService
    {
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        Task<IEnumerable<IdentityUserRole<string>>> GetUserRolesAsync(string email);
        Task AddUserToRoleAsync(string email, string roleName);
    }

    public class UserRoleService : IUserRoleService
    {
        private readonly ILogger<UserRoleService> _logger;
        private readonly ApplicationDbContext _db;

        public UserRoleService(ILogger<UserRoleService> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync()
        {
            var users = _db.Users.ToList();
            return await Task.FromResult(users);
        }

        public async Task AddUserToRoleAsync(string email, string roleName)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            var role = _db.Roles.FirstOrDefault(r => r.Name == roleName);
            if (user != null && role != null && !_db.UserRoles.Any(usr => usr.UserId == user.Id && usr.RoleId == role.Id))
            {
                _logger.LogDebug("Adding user {Email} to role {RoleName}", email, roleName);
                _db.UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = role.Id,
                    UserId = user.Id
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task RemoveUserFromRoleAsync(string email, string roleName)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email) ?? throw new ArgumentException("Invalid user");
            var role = _db.Roles.FirstOrDefault(r => r.Name == roleName) ?? throw new ArgumentException("Invalid role");
            var userRole = _db.UserRoles.FirstOrDefault(userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id) ?? throw new ArgumentException("User not assigned to role");
            _logger.LogDebug("Removing user {Email} from role {RoleName}", email, roleName);
            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<IdentityUserRole<string>>> GetUserRolesAsync(string email)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email) ?? throw new ArgumentException("Invalid User");

            var roles = _db.UserRoles.Where(userRole => userRole.UserId == user.Id).AsEnumerable();

            return roles;
        }
    }
}
