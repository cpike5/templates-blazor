using BlazorTemplate.Configuration.Navigation;
using BlazorTemplate.Data;
using BlazorTemplate.Services;

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
            return services;
        }
    }
}
