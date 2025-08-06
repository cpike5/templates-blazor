using BlazorTemplate.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public async Task SeedDatabase()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<ApplicationDbContext>() ?? throw new ArgumentException("ApplicationDbContext not registered");
                var config = services.GetRequiredService<IOptions<ConfigurationOptions>>() ?? throw new ArgumentException("ConfigurationOptions not registered");
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                _logger.LogDebug("Seeding database");

                foreach (var roleName in config.Value.Administration.UserRoles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        _logger.LogDebug("Adding missing database role: {RoleName}", roleName);
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Set the admin
                var adminUser = await userManager.FindByEmailAsync(config.Value.Administration.AdminEmail);
                if (adminUser == null)
                {
                    _logger.LogWarning("Admin user {Email} doesn't exist - skipping admin role configuratrion", config.Value.Administration.AdminEmail);
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(adminUser, config.Value.Administration.AdministratorRole))
                    {
                        await userManager.AddToRoleAsync(adminUser, config.Value.Administration.AdministratorRole);
                    }
                }
                
                // Create the Guest User if enabled
                if (config.Value.Setup.EnableGuestUser)
                {
                    _logger.LogInformation("Guest Account enabled");
                    var guest = config.Value.Setup.GuestUser;
                    var guestUser = await userManager.FindByEmailAsync(guest.Email);
                    if (guestUser == null)
                    {
                        _logger.LogInformation("Guest account is enabled but configured account does not exist.");
                        try
                        {
                            _logger.LogInformation("Creating guest account with email: {Email}", guest.Email);
                            _logger.LogWarning("GUEST USER CREDENTIALS - Email: {Email}, Password: {Password}", guest.Email, guest.Password);
                            Console.WriteLine($"=== GUEST USER CREDENTIALS ===");
                            Console.WriteLine($"Email: {guest.Email}");
                            Console.WriteLine($"Password: {guest.Password}");
                            Console.WriteLine($"===============================");
                            
                            var newUser = await userManager.CreateAsync(new ApplicationUser
                            {
                                UserName = guest.Email,
                                Email = guest.Email,
                                EmailConfirmed = true,
                                LockoutEnabled = false
                            }, guest.Password);
                            if (newUser.Succeeded)
                            {
                                _logger.LogInformation("Guest account {Email} created successfully", guest.Email);
                            }
                            else
                            {
                                var errors = string.Join(", ", newUser.Errors.Select(error => $"{error.Code} - {error.Description}"));
                                throw new InvalidOperationException(errors);
                            }

                            // Create the guest role if needed
                            var guestRoleName = "Guest";
                            if (!await roleManager.RoleExistsAsync(guestRoleName))
                            {
                                _logger.LogInformation("Creating guest role: {RoleName}", guestRoleName);
                                var roleResult = await roleManager.CreateAsync(new IdentityRole(guestRoleName));
                                if (!roleResult.Succeeded)
                                {
                                    var roleErrors = string.Join(", ", roleResult.Errors.Select(error => $"{error.Code} - {error.Description}"));
                                    throw new InvalidOperationException($"Failed to create guest role: {roleErrors}");
                                }
                                
                                // Assign the guest user to the guest role
                                var createdUser = await userManager.FindByEmailAsync(guest.Email);
                                if (createdUser != null)
                                {
                                    var isInRole = await userManager.IsInRoleAsync(createdUser, guestRoleName);
                                    if (!isInRole)
                                    {
                                        _logger.LogInformation("Adding guest user to guest role");
                                        var addToRoleResult = await userManager.AddToRoleAsync(createdUser, guestRoleName);
                                        if (!addToRoleResult.Succeeded)
                                        {
                                            var roleErrors = string.Join(", ", addToRoleResult.Errors.Select(error => $"{error.Code} - {error.Description}"));
                                            _logger.LogWarning("Failed to add guest user to role: {Errors}", roleErrors);
                                        }
                                        else
                                        {
                                            _logger.LogInformation("Guest user successfully added to guest role");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error creating guest account");
                        }
                    }
                }

                await db.SaveChangesAsync();
                _logger.LogTrace("Finished seeding database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while attempting to seed database");
            }
        }

    }
}
