using BlazorTemplate.Configuration.Navigation;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using BlazorTemplate.Services.Auth;
using BlazorTemplate.Services.Invites;
using BlazorTemplate.Services.Navigation;
using BlazorTemplate.Services.Setup;
using BlazorTemplate.Services.Monitoring;
using BlazorTemplate.Services.Media;

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

        public static IServiceCollection AddFileManagementServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Get configuration options to determine storage provider
            var siteConfig = configuration.GetSection(ConfigurationOptions.SectionName).Get<ConfigurationOptions>();
            var storageProvider = siteConfig?.FileManagement?.LocalStorage?.RootPath != null ? "Local" : "Local";
            
            // Storage Services - for Phase 1, we only support Local storage
            switch (storageProvider.ToLower())
            {
                case "local":
                    services.AddSingleton<IMediaStorageService, LocalFileStorageService>();
                    break;
                // Future providers can be added here:
                // case "azureblob":
                //     services.AddSingleton<IMediaStorageService, AzureBlobStorageService>();
                //     break;
                // case "aws":
                // case "s3":
                //     services.AddSingleton<IMediaStorageService, AwsS3StorageService>();
                //     break;
                default:
                    services.AddSingleton<IMediaStorageService, LocalFileStorageService>();
                    break;
            }

            // Core Services
            services.AddScoped<IMediaManagementService, MediaManagementService>();
            services.AddScoped<IFileSecurityService, FileSecurityService>();
            
            return services;
        }

    }
}
