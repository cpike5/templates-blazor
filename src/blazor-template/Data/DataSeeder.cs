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
        private readonly IServiceProvider services;

        public DataSeeder(IServiceProvider serviceProvider)
        {
            services = serviceProvider;
            _logger = services.GetRequiredService<ILogger<DataSeeder>>();
        }

        /// <summary>
        /// Seeds the database with initial user role data
        /// </summary>
        public static void SeedDatabase(IServiceProvider services)
        {
            var seeder = new DataSeeder(services);
            seeder.SeedDatabase();
        }

        private void SeedDatabase()
        {
            try
            {
                var db = services.GetRequiredService<ApplicationDbContext>();
                var config = services.GetRequiredService<IOptions<ConfigurationOptions>>();

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
