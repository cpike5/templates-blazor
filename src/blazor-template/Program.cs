using BlazorTemplate.Components;
using BlazorTemplate.Components.Account;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using BlazorTemplate.Extensions;
using BlazorTemplate.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BlazorTemplate
{
    public class Program
    {
        private static ILogger<Program> _logger;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            bool firstTimeSetup = Convert.ToBoolean(builder.Configuration["Site:Setup:EnableSetupMode"]);

            // Configure logging
            builder.Services.AddSerilog(logger =>
            {
                logger.ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(le => le.Level >= Serilog.Events.LogEventLevel.Warning)
                        .WriteTo.File(
                            path: "logs/warnings-.txt",
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));
            });

            // Configure Site Options
            builder.Services.Configure<ConfigurationOptions>(builder.Configuration.GetSection(ConfigurationOptions.SectionName));
            builder.Services.AddNavigationServices(builder.Configuration);
            builder.Services.AddAdminServices();
            builder.Services.AddScoped<ThemeService>();
            
            // Settings services
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<ISettingsEncryptionService, SettingsEncryptionService>();
            builder.Services.AddMemoryCache();
            
            // UI services
            builder.Services.AddScoped<IToastService, ToastService>();
            
            // Background services
            builder.Services.AddHostedService<SystemHealthBackgroundService>();


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

            authBuilder.AddIdentityCookies();

            if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]) && !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientSecret"]))
            {
                authBuilder.AddGoogle(a =>
                {
                    a.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                    a.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                });
            }

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            // Register DbContext for Identity and regular use
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Register DbContextFactory for services that need concurrent access with independent configuration
            builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(provider =>
            {
                return new ApplicationDbContextFactory(connectionString);
            });
            
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            builder.Services.AddAuthorization();


            if (firstTimeSetup)
            {
                builder.Services.AddFirstTimeSetupServices(builder.Configuration);
            }            

            var app = builder.Build();

            using var scope = app.Services.CreateScope();

            // Get a reference to the DB Context
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (firstTimeSetup)
            {
                var setupService = scope.ServiceProvider.GetRequiredService<IFirstTimeSetupService>();
                setupService.Setup();
            }

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
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }


    }
}
