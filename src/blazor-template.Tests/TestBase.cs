using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlazorTemplate.Data;
using BlazorTemplate.Configuration;
using System.Security.Claims;

namespace blazor_template.Tests.Infrastructure;

/// <summary>
/// Base test class providing common test infrastructure and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ServiceProvider ServiceProvider { get; private set; }
    protected ApplicationDbContext DbContext { get; private set; }
    protected UserManager<ApplicationUser> UserManager { get; private set; }
    protected RoleManager<IdentityRole> RoleManager { get; private set; }
    protected IConfiguration Configuration { get; private set; }

    protected TestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UserManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Ensure database is created for each test
        DbContext.Database.EnsureCreated();
        
        // Call setup completion hook
        OnSetupComplete();
    }

    protected virtual void OnSetupComplete()
    {
        // Override in derived classes for additional setup after services are configured
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Configure in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add configuration
        var configurationData = new Dictionary<string, string?>
        {
            ["Site:Name"] = "Test Application",
            ["Site:Setup:EnableSetupMode"] = "true",
            ["Site:Setup:EnableGuestUser"] = "false",
            ["Site:Setup:EnableInviteOnlyRegistration"] = "false",
            ["Site:Administration:AdminEmail"] = "admin@test.com",
            ["Site:Administration:DefaultRoles:0"] = "Admin",
            ["Site:Administration:DefaultRoles:1"] = "User",
            ["Site:Administration:DefaultRoles:2"] = "Guest",
            ["Navigation:Items:0:Name"] = "Home",
            ["Navigation:Items:0:Path"] = "/",
            ["Navigation:Items:0:Icon"] = "fas fa-home",
            ["Navigation:Items:0:Order"] = "1",
            ["Navigation:Items:0:RequiredRoles:0"] = "User",
            ["Navigation:Items:1:Name"] = "Admin",
            ["Navigation:Items:1:Path"] = "/admin",
            ["Navigation:Items:1:Icon"] = "fas fa-cog",
            ["Navigation:Items:1:Order"] = "2",
            ["Navigation:Items:1:RequiredRoles:0"] = "Admin"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<ConfigurationOptions>(configuration);

        // Add logging
        services.AddLogging(builder => builder.AddConsole());
    }

    protected async Task<ApplicationUser> CreateTestUserAsync(string email = "test@example.com", string password = "TestPassword123!", params string[] roles)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        foreach (var role in roles)
        {
            await EnsureRoleExistsAsync(role);
            await UserManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    protected async Task<IdentityRole> EnsureRoleExistsAsync(string roleName)
    {
        var role = await RoleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            await RoleManager.CreateAsync(role);
        }
        return role;
    }

    protected ClaimsPrincipal CreateClaimsPrincipal(ApplicationUser user, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public virtual void Dispose()
    {
        DbContext?.Dispose();
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
