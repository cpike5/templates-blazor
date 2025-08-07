using BlazorTemplate.Configuration.Navigation;
using BlazorTemplate.Data;
using BlazorTemplate.Services.Auth;
using BlazorTemplate.Services.Invites;
using BlazorTemplate.Services.Navigation;
using BlazorTemplate.Services.Setup;
using BlazorTemplate.Services.Monitoring;

namespace BlazorTemplate.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNavigationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<NavigationConfiguration>(configuration.GetSection(NavigationConfiguration.SectionName));
            services.AddScoped<INavigationService, NavigationService>();
            return services;
        }

        public static IServiceCollection AddFirstTimeSetupServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IFirstTimeSetupService, FirstTimeSetupService>();
            services.AddScoped<DataSeeder>();
            return services;
        }

        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {            
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IAdminRoleService, AdminRoleService>();
            services.AddScoped<IInviteService, InviteService>();
            services.AddScoped<IEmailInviteService, EmailInviteService>();
            services.AddScoped<ISystemMonitoringService, SystemMonitoringService>();
            services.AddScoped<IDashboardService, DashboardService>();
            return services;
        }

    }
}
