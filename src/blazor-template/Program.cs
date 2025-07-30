using BlazorTemplate.Components;
using BlazorTemplate.Components.Account;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using BlazorTemplate.Extensions;
using Microsoft.Extensions.Options;
using BlazorTemplate.Services;

namespace BlazorTemplate
{
    public class Program
    {
        private static ILogger<Program> _logger;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Services.AddSerilog(logger =>
            {
                logger.ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            // Configure Site Options
            builder.Services.Configure<ConfigurationOptions>(builder.Configuration.GetSection(ConfigurationOptions.SectionName));

            builder.Services.AddNavigationServices(builder.Configuration);

            builder.Services.AddScoped<IUserRoleService, UserRoleService>();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            var authBuilder = builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                });

            if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]) && !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientSecret"]))
            {
                authBuilder.AddGoogle(a =>
                {
                    a.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                    a.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                });
            }
            authBuilder.AddIdentityCookies();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            var app = builder.Build();


            // Get a reference to the DB Context
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Get a logger 
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            SeedDatabase(scope.ServiceProvider);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }

        private static void SeedDatabase(IServiceProvider services)
        {
            try
            {
                var db = services.GetRequiredService<ApplicationDbContext>();
                var config = services.GetRequiredService<IOptions<ConfigurationOptions>>();

                _logger.LogDebug("Seeding database");
                
                foreach (var roleName in GetApplicationRoles())
                {
                    if (!db.Roles.Any(role => role.Name == roleName))
                    {
                        _logger.LogDebug("Adding missing database role: {RoleName}", roleName);
                        db.Roles.Add(new IdentityRole(roleName));
                    }
                }

                // Set the admin
                var adminRole = db.Roles.SingleOrDefault(role => role.Name == "Administrator");
                var adminUser = db.Users.SingleOrDefault(user => user.Email == config.Value.AdminEmail);
                if (adminRole != null && adminUser != null && !db.UserRoles.Any(userRole => userRole.UserId == adminUser.Id && userRole.RoleId == adminRole.Id))
                {
                    _logger.LogDebug("Adding admin user {Email} to Administrator role", adminUser.Email);
                    db.UserRoles.Add(new IdentityUserRole<string>
                    {
                        RoleId = adminRole.Id,
                        UserId = adminUser.Id
                    });
                }

                db.SaveChangesAsync();
                _logger.LogTrace("Finished seeding database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while attempting to seed database");
            }
        }

        private static IEnumerable<string> GetApplicationRoles()
        {
            var roles = new List<string>()
            {
                "Administrator",
                "User"
            };

            return roles;
        }
    }
}
