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

                // Create the Guest User if enabled
                if (config.Value.Setup.EnableGuestUser)
                {
                    _logger.LogInformation("Guest Account enabled");
                    var guest = config.Value.Setup.GuestUser;
                    var guestUser = db.Users.SingleOrDefault(user => user.Email == guest.Email);
                    if (guestUser == null)
                    {
                        _logger.LogInformation("Guest account is enabled but configured account does not exist.");
                        try
                        {
                            _logger.LogInformation("Creating guest account");
                            var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                            var newUser = userManager.CreateAsync(new ApplicationUser
                            {
                                UserName = guest.Email,
                                Email = guest.Email,
                                EmailConfirmed = true,
                                LockoutEnabled = false
                            }, guest.Password).GetAwaiter().GetResult();
                            if (newUser.Succeeded)
                            {
                                _logger.LogInformation("Guest account {Email} created successfully", guest.Email);
                            }
                            else
                            {
                                var errors = string.Join(", ", newUser.Errors.Select(error => $"{error.Code} - {error.Description}"));
                                throw new InvalidOperationException(errors);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error creating guest account");
                        }
                    }
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
