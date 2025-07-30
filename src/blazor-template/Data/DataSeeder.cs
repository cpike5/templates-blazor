using BlazorTemplate.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlazorTemplate.Data
{
    /// <summary>
    /// Utility class to help seeding the application with initial data
    /// </summary>
    public class DataSeeder
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public DataSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DataSeeder>>();
        }
        
        public void SeedDatabase()
        {
            try
            {
                var db = _serviceProvider.GetRequiredService<ApplicationDbContext>() ?? throw new ArgumentException("ApplicationDbContext not registered");
                var config = _serviceProvider.GetRequiredService<IOptions<ConfigurationOptions>>() ?? throw new ArgumentException("ConfigurationOptions not registered");

                _logger.LogDebug("Seeding database");

                foreach (var roleName in config.Value.Administration.UserRoles)
                {
                    if (!db.Roles.Any(role => role.Name == roleName))
                    {
                        _logger.LogDebug("Adding missing database role: {RoleName}", roleName);
                        db.Roles.Add(new IdentityRole(roleName));
                    }
                }
                db.SaveChanges();

                // Set the admin
                var adminRole = db.Roles.SingleOrDefault(role => role.Name == config.Value.Administration.AdministratorRole);
                var adminUser = db.Users.SingleOrDefault(user => user.Email == config.Value.Administration.AdminEmail);
                if (adminRole != null && adminUser != null && !db.UserRoles.Any(userRole => userRole.UserId == adminUser.Id && userRole.RoleId == adminRole.Id))
                {
                    _logger.LogDebug("Adding admin user {Email} to Administrator role", adminUser.Email);
                    db.UserRoles.Add(new IdentityUserRole<string>
                    {
                        RoleId = adminRole.Id,
                        UserId = adminUser.Id
                    });
                }

                db.SaveChanges();
                _logger.LogTrace("Finished seeding database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while attempting to seed database");
            }
        }

    }
}
